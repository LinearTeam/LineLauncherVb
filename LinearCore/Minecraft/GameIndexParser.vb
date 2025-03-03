﻿Imports System.IO
Imports LinearCore.JsonUtils

Public Class MinecraftFile
    Public url As String
    Public path As String
    Public sha1 As String
    Public isNative As Boolean = False
    Public isResources As Boolean = False
    Public isClient As Boolean = False
End Class

Public Class GameIndexParser
    Private Shared Function IsRightForCurrentPlatform(item As String) As Boolean
        Dim jsonParser = New JsonUtils(item)
        Dim isAllow As Boolean = False
        '获取所有键，是否包含 rules
        If GetValueFromJson(item, "rules") IsNot Nothing Then
            For Each i In jsonParser.GetArray("rules")
                ' 获取 action 和对应的平台
                Dim action As String = GetValueFromJson(i.ToString(), "action")
                Dim platform = GetValueFromJson(i.ToString(), "os.name")
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
        If GetValueFromJson(item, "natives") IsNot Nothing Then
            If GetValueFromJson(item, "natives.windows") IsNot Nothing Then
                Return True
            End If
        End If

        '新版中移除了 classifiers，如果上面的 If 未返回，则根据名称来区分
        Dim path = GetValueFromJson(item, "downloads.artifact.path")
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
        Dim indexId = JsonUtils.GetValueFromJson(content, "assetIndex.id")
        Dim pathToResourcesIndex = $"{root}\assets\indexes\{indexId}.json"
        Dim jsonParser = New JsonUtils(content)
        Dim _hostProvider = New HostProvider(mirror)

        Dim libraries = New List(Of MinecraftFile)
        For Each i In jsonParser.GetArray("libraries")
            If IsRightForCurrentPlatform(i.ToString()) Then '检查是否是 windows 平台
                If IsNatives(i.ToString()) Then '检查是否是 natives
                    Dim native As New MinecraftFile()
                    Dim classifierStr = GetValueFromJson(i.ToString(), $"downloads.classifiers")
                    Dim classifiersPath = GetValueFromJson(classifierStr, "natives-windows.path")

                    If classifiersPath IsNot Nothing Then '首先确定 classifiers 不为空（路径不为空代表它不为空）
                        If IsRightArchitecture(classifiersPath) Then '检查是否是 x64 架构
                            native.path = $"{root}\libraries\{classifiersPath}"
                            native.sha1 = GetValueFromJson(classifierStr, "natives-windows.sha1")
                            native.url = _hostProvider.TransformToTargetMirror(GetValueFromJson(classifierStr, "natives-windows.url"))
                            native.isNative = True
                            libraries.Add(native)
                        End If
                        Continue For '无论符不符合架构，凡是扫到 classifiers 并处理后，退出本轮循环
                    End If

                    '如果在前面并没有退出，那么说明是新版本
                    Dim artifactLikeClassifiersPath = GetValueFromJson(i.ToString(), "downloads.artifact.path")

                    If artifactLikeClassifiersPath IsNot Nothing Then '判断路径不为空，整体不为空
                        If IsRightArchitecture(artifactLikeClassifiersPath) Then '检查架构
                            native.path = $"{root}\libraries\{artifactLikeClassifiersPath}"
                            native.sha1 = GetValueFromJson(i.ToString(), "downloads.artifact.sha1")
                            native.url = _hostProvider.TransformToTargetMirror(GetValueFromJson(i.ToString(), "downloads.artifact.url"))
                            native.isNative = True
                            libraries.Add(native)
                        End If
                    End If
                    Continue For '避免新版本和下面的 artifact 重复判断，一旦确定这个元素是 natives，处理后立马退出本轮循环
                End If

                'artifact 判断逻辑
                Dim library As New MinecraftFile()
                Dim artifactStr = GetValueFromJson(i.ToString(), "downloads.artifact")
                Dim artifactPath = GetValueFromJson(artifactStr, "path")
                If artifactPath IsNot Nothing Then
                    If IsRightArchitecture(artifactPath) Then
                        library.path = $"{root}\libraries\{artifactPath}"
                        library.sha1 = GetValueFromJson(artifactStr, "sha1")
                        library.url = _hostProvider.TransformToTargetMirror(GetValueFromJson(artifactStr, "url"))
                        libraries.Add(library)
                    End If
                End If
            End If
        Next

        '资源判断逻辑
        Dim resources = New List(Of MinecraftFile)
        Dim resJsonParser = New JsonUtils(File.ReadAllText(pathToResourcesIndex))
        Dim keys = resJsonParser.GetNestedKeys("objects")
        For Each i In keys
            Dim res As New MinecraftFile()
            Dim hash = resJsonParser.GetNestedValue($"objects|{i}|hash", "|")
            Dim path = $"{root}\assets\objects\{Left(hash, 2)}\{hash}"
            Dim url = $"{_hostProvider.provideMirror.resources}/{Left(hash, 2)}/{hash}"

            res.path = path
            res.sha1 = hash
            res.url = url
            res.isResources = True
            resources.Add(res)
        Next

        libraries.AddRange(resources)

        '版本jar
        Dim versionJar As New MinecraftFile With {
            .path = $"{root}/versions/{customName}/{customName}.jar",
            .sha1 = GetValueFromJson(content, "downloads.client.sha1"),
            .url = _hostProvider.TransformToTargetMirror(GetValueFromJson(content, "downloads.client.url")),
            .isClient = True
        }
        libraries.Add(versionJar)
        Return libraries
    End Function
End Class