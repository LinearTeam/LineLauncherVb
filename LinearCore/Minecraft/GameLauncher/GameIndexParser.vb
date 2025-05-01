Imports System.IO
Imports LinearCore.JsonUtils

Public Class GameIndexParser
    Private Shared Function IsRightForCurrentPlatform(item As String) As Boolean
        Dim parsedItem = JsonUtils.Parse(item)
        Dim isAllow As Boolean = False
        '获取所有键，是否包含 rules
        If parsedItem.rules IsNot Nothing Then
            For Each rule In parsedItem.rules
                ' 获取 action 和对应的平台
                Dim action = rule.action
                Dim platform = rule.os?.name
                ' 仅当平台存在时剖析 rules，否则略过
                If platform IsNot Nothing Then
                    ' 如果 allow 直接说明 windows，则返回 True
                    If action = "allow" Then
                        If platform = "windows" Then
                            Return True
                        End If
                    Else
                        ' 如果 action 为 False 且直接说明 windows，返回 False
                        If platform = "windows" Then
                            Return False
                        End If
                        ' 如果 action 为 False 但平台不是 windows，将 isAllow 标记为 True
                        isAllow = True
                    End If
                End If
            Next
            Return isAllow ' 如果方法在前面的分支中没有返回，则直接返回 isAllow
        End If
        ' 如果不包含 rules 则可启用
        Return True
    End Function

    '仅 x64 平台 dll
    Private Shared Function IsRightArchitecture(path As String) As Boolean
        If path.Contains("x86") Or path.Contains("arm") Then
            Return False
        End If
        Return True
    End Function

    '检查是否为 natives 库
    Private Shared Function IsNatives(item As String) As Boolean
        '在旧版本中，natives 存储在 classifiers 键下，碰到返回
        Dim parsedItem = JsonUtils.Parse(item)
        If parsedItem.natives IsNot Nothing Then
            If parsedItem.natives.windows IsNot Nothing Then
                Return True
            End If
        End If

        '新版中移除了 classifiers，如果上面的 If 未返回，则根据名称来区分
        Dim path = parsedItem.downloads.artifact.path
        If path.Contains("natives") Then
            Return True
        End If
        Return False
    End Function

    ''' <summary>
    ''' 解析一个特定版本的版本 Json 文件
    ''' </summary>
    ''' <param name="pathToIndex">到索引文件的路径</param>
    ''' <param name="root">.minecraft 根目录</param>
    ''' <returns></returns>
    Public Shared Function ParseIndex(customName As String, pathToVersionIndex As String, root As String, mirror As String) As List(Of MinecraftFile)
        Dim content = File.ReadAllText(pathToVersionIndex)
        Dim parsedContent = JsonUtils.Parse(content)

        Dim indexId = parsedContent.assetIndex.id
        Dim pathToResourcesIndex = $"{root}\assets\indexes\{indexId}.json"

        Dim _hostProvider = New HostProvider(mirror)

        Dim libraries = New List(Of MinecraftFile)
        For Each library In parsedContent.libraries
            Dim serializedLib = JsonUtils.Serialize(library)

            If Not serializedLib.Contains("downloads") Then '判断第三方 mod 加载器文件
                Dim modLoaderFile As New MinecraftFile() With {
                    .path = $"{root}\libraries\{library.path}",
                    .sha1 = Nothing,
                    .url = Nothing,
                    .doNotDownload = True
                }
                libraries.Add(modLoaderFile)
                Continue For
            End If

            If IsRightForCurrentPlatform(serializedLib) Then '检查是否是 windows 平台
                If IsNatives(serializedLib) Then '检查是否是 natives

                    Dim native As New MinecraftFile()

                    Dim classifiersPath = library("downloads")("classifiers")("natives-windows")("path")

                    If classifiersPath IsNot Nothing Then '首先确定 classifiers 不为空（路径不为空代表它不为空）
                        If IsRightArchitecture(classifiersPath) Then '检查是否是 x64 架构
                            native.path = $"{root}\libraries\{classifiersPath}"
                            native.sha1 = library("downloads")("classifiers")("natives-windows")("sha1")
                            native.url = _hostProvider.TransformToTargetMirror(library("downloads")("classifiers")("natives-windows")("url"))
                            native.isNative = True
                            libraries.Add(native)
                        End If
                        Continue For '无论符不符合架构，凡是扫到 classifiers 并处理后，退出本轮循环
                    End If

                    '如果在前面并没有退出，那么说明是新版本
                    Dim artifactLikeClassifiersPath = library.downloads.artifact.path

                    If artifactLikeClassifiersPath IsNot Nothing Then '判断路径不为空，整体不为空
                        If IsRightArchitecture(artifactLikeClassifiersPath) Then '检查架构
                            native.path = $"{root}\libraries\{artifactLikeClassifiersPath}"
                            native.sha1 = library.downloads.artifact.sha1
                            native.url = _hostProvider.TransformToTargetMirror(library.downloads.artifact.url)
                            native.isNative = True
                            libraries.Add(native)
                        End If
                    End If
                    Continue For '避免新版本和下面的 artifact 重复判断，一旦确定这个元素是 natives，处理后立马退出本轮循环
                End If

                'artifact 判断逻辑
                Dim artifactPath = library.downloads.artifact.path
                If artifactPath IsNot Nothing Then
                    If IsRightArchitecture(artifactPath) Then
                        If library.downloads.artifact.url = "" Then
                            Continue For
                        End If
                        Dim artifactLibrary As New MinecraftFile() With {
                            .path = $"{root}\libraries\{artifactPath}",
                            .sha1 = library.downloads.artifact.sha1,
                            .url = _hostProvider.TransformToTargetMirror(library.downloads.artifact.url)
                        }

                        libraries.Add(artifactLibrary)
                    End If
                End If
            End If
        Next

        '资源判断逻辑
        Dim resources = New List(Of MinecraftFile)

        Dim parsedResources = JsonUtils.Parse(File.ReadAllText(pathToResourcesIndex))
        Dim keys = parsedResources.GetNestedKeys("objects")
        For Each key In keys
            Dim hash = parsedResources("objects")(key)("hash")
            Dim path = $"{root}\assets\objects\{Left(hash, 2)}\{hash}"
            Dim url = $"{_hostProvider.provideMirror.resources}/{Left(hash, 2)}/{hash}"

            Dim res As New MinecraftFile() With {
                .path = path,
                .sha1 = hash,
                .url = url,
                .isResources = True
            }

            resources.Add(res)
        Next

        libraries.AddRange(resources)

        '版本jar
        Dim versionJar As New MinecraftFile With {
            .path = $"{root}/versions/{customName}/{customName}.jar",
            .sha1 = parsedContent.downloads.client.sha1,
            .url = _hostProvider.TransformToTargetMirror(parsedContent.downloads.client.url),
            .isClient = True
        }
        libraries.Add(versionJar)
        Return libraries
    End Function
End Class