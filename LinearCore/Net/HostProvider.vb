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

''' <summary>
''' 根据 Json 中的配置文件提供所需源的信息
''' </summary>
Public Class HostProvider
    Public Mirrors As New Dictionary(Of String, String)

    Private ReadOnly JsonParser As JsonUtils
    ''' <summary>
    ''' 实例化一个 HostProvider
    ''' </summary>
    ''' <param name="mirror">镜像源名称</param>
    Public Sub New(mirror As String)
        Dim assembly As Assembly = Assembly.GetExecutingAssembly()
        Using stream As Stream = assembly.GetManifestResourceStream("LinearCore.MirrorConfig.json")
            Using reader As New StreamReader(stream)
                Dim content As String = reader.ReadToEnd()
                JsonParser = New JsonUtils((content))
                Dim mirrorsList = JsonParser.GetNestedKeys("mirrors")
                If mirrorsList.Contains(mirror) Then
                    Mirrors.Add("pistonMeta", JsonParser.GetNestedValue($"mirrors.{mirror}.pistonMeta"))
                    Mirrors.Add("pistonData", JsonParser.GetNestedValue($"mirrors.{mirror}.pistonData"))
                    Mirrors.Add("launcherMeta", JsonParser.GetNestedValue($"mirrors.{mirror}.launcherMeta"))
                    Mirrors.Add("launcher", JsonParser.GetNestedValue($"mirrors.{mirror}.launcher"))
                    Mirrors.Add("resources", JsonParser.GetNestedValue($"mirrors.{mirror}.resources"))
                    Mirrors.Add("libraries", JsonParser.GetNestedValue($"mirrors.{mirror}.libraries"))
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
                Return $"{Mirrors("pistonMeta")}{pathUrl}"
            Case "piston-data.mojang"
                Return $"{Mirrors("pistonData")}{pathUrl}"
            Case "launchermeta.mojang"
                Return $"{Mirrors("launcherMeta")}{pathUrl}"
            Case "launcher.mojang"
                Return $"{Mirrors("launcher")}{pathUrl}"
            Case "resources.download.minecraft"
                Return $"{Mirrors("resources")}{pathUrl}"
            Case "libraries.minecraft"
                Return $"{Mirrors("libraries")}{pathUrl}"
            Case Else
                Throw New Exception($"Illegal url: {url}")
        End Select
    End Function
End Class