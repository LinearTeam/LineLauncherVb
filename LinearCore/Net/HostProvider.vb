Imports System.IO
Imports System.Reflection

Public Class UndefinedMirrorException
    Inherits Exception
    Public Sub New()
        MyBase.New("Undefined or incompleted mirror")
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class

Public Class Mirror
    Public pistonMeta As String
    Public pistonData As String
    Public laucherMeta As String
    Public launcher As String
    Public resources As String
    Public libraries As String

    Public fabric As String
End Class

''' <summary>
''' 根据 Json 中的配置文件提供所需源的信息
''' </summary>
Public Class HostProvider
    Public provideMirror As New Mirror()
    Private ReadOnly _jsonParser As JsonUtils

    ''' <summary>
    ''' 实例化一个 HostProvider
    ''' </summary>
    ''' <param name="mirror">镜像源名称</param>
    Public Sub New(mirror As String)
        Dim assembly As Assembly = Assembly.GetExecutingAssembly()
        Using stream As Stream = assembly.GetManifestResourceStream("LinearCore.MirrorConfig.json")
            Using reader As New StreamReader(stream)
                Dim content As String = reader.ReadToEnd()
                _jsonParser = New JsonUtils((content))
                Dim mirrorsList = _jsonParser.GetNestedKeys("mirrors")
                If mirrorsList.Contains(mirror) Then
                    provideMirror.pistonMeta = _jsonParser.GetNestedValue($"mirrors.{mirror}.pistonMeta")
                    provideMirror.pistonData = _jsonParser.GetNestedValue($"mirrors.{mirror}.pistonData")
                    provideMirror.laucherMeta = _jsonParser.GetNestedValue($"mirrors.{mirror}.launcherMeta")
                    provideMirror.launcher = _jsonParser.GetNestedValue($"mirrors.{mirror}.launcher")
                    provideMirror.resources = _jsonParser.GetNestedValue($"mirrors.{mirror}.resources")
                    provideMirror.libraries = _jsonParser.GetNestedValue($"mirrors.{mirror}.libraries")
                    provideMirror.fabric = _jsonParser.GetNestedValue($"mirrors.{mirror}.fabric")
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
            Case Else
                Throw New Exception($"Illegal url: {url}")
        End Select
    End Function
End Class