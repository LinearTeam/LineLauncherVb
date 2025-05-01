Imports System.IO
Imports System.Reflection

''' <summary>
''' 根据 Json 中的配置文件提供所需源的信息
''' </summary>
Public Class HostProvider
    Public provideMirror As New Mirror()
    Private ReadOnly _parsedContent
    ''' <summary>
    ''' 实例化一个 HostProvider
    ''' </summary>
    ''' <param name="mirror">镜像源名称</param>
    Public Sub New(mirror As String)
        Dim assembly As Assembly = Assembly.GetExecutingAssembly()
        Using stream As Stream = assembly.GetManifestResourceStream("LinearCore.MirrorConfig.json")
            Using reader As New StreamReader(stream)
                Dim content As String = reader.ReadToEnd()
                _parsedContent = JsonUtils.Parse(content)

                Dim mirrorsList = _parsedContent.GetNestedKeys("mirrors")
                If mirrorsList.Contains(mirror) Then
                    provideMirror.pistonMeta = _parsedContent("mirrors")(mirror)("pistonMeta")
                    provideMirror.pistonData = _parsedContent("mirrors")(mirror)("pistonData")
                    provideMirror.laucherMeta = _parsedContent("mirrors")(mirror)("launcherMeta")
                    provideMirror.launcher = _parsedContent("mirrors")(mirror)("launcher")
                    provideMirror.resources = _parsedContent("mirrors")(mirror)("resources")
                    provideMirror.libraries = _parsedContent("mirrors")(mirror)("libraries")
                    provideMirror.fabric = _parsedContent("mirrors")(mirror)("fabric")
                    provideMirror.forgeMaven = _parsedContent("mirrors")(mirror)("forgeMaven")
                Else
                    Throw New UndefinedMirrorException()
                End If
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' 将原 Json 的 url 转换到镜像源
    ''' </summary>
    ''' <param name="url">原 url</param>
    ''' <returns>等效于镜像源中的url</returns>
    Public Function TransformToTargetMirror(url As String)
        Dim hostUrl = url.Split("//")(1).Split("/")(0)
        Dim pathUrl = url.Replace($"https://{hostUrl}", "")
        Dim domain = hostUrl.Split(".").Reverse()(0)
        hostUrl = hostUrl.Replace($".{domain}", "")

        Select Case hostUrl
            Case "piston-meta.mojang"
                Return $"{provideMirror.pistonMeta}{pathUrl}"
            Case "piston-data.mojang"
                Return $"{provideMirror.pistonData}{pathUrl}"
            Case "launchermeta.mojang"
                Return $"{provideMirror.laucherMeta}{pathUrl}"
            Case "launcher.mojang"
                Return $"{provideMirror.launcher}{pathUrl}"
            Case "resources.download.minecraft"
                Return $"{provideMirror.resources}{pathUrl}"
            Case "libraries.minecraft"
                Return $"{provideMirror.libraries}{pathUrl}"
            Case "maven.minecraftforge"
                Return $"{provideMirror.forgeMaven}{pathUrl}"
            Case Else
                Throw New Exception($"Illegal url: {url}")
        End Select
    End Function
End Class