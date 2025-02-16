Imports System.Net
Imports System.Diagnostics
Imports System.Collections.Specialized
Imports System.Web

Public Class LocalServerProvider
    Implements IDisposable

    Private Shared _code As String
    Private ReadOnly _listener As HttpListener
    Private _disposed As Boolean = False

    Public Sub New()
        _listener = New HttpListener()
    End Sub

    ''' <summary>
    ''' 启动本地服务器并等待授权码
    ''' </summary>
    Public Sub StartServer()
        Try
            ' 设置监听地址
            Dim url As String = "http://127.0.0.1:40935/"
            _listener.Prefixes.Add(url)
            _listener.Start()

            ' 打开浏览器进行用户授权
            OpenBrowserForAuthorization()

            ' 等待用户授权并获取授权码
            WaitForAuthorizationCode()

            ' 返回成功页面
            SendSuccessResponse()
        Catch ex As Exception
            ' 捕获并处理异常
            Console.WriteLine($"Failed to start server: {ex.Message}")
            Throw
        Finally
            ' 确保服务器停止
            If _listener.IsListening Then
                _listener.Stop()
            End If
        End Try
    End Sub

    ''' <summary>
    ''' 打开浏览器进行用户授权
    ''' </summary>
    Private Shared Sub OpenBrowserForAuthorization()
        Dim authUrl As String = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize" &
                                "?client_id=1cbfda79-fc84-47f9-8110-f924da9841ec" &
                                "&response_type=code" &
                                "&redirect_uri=http://127.0.0.1:40935" &
                                "&response_mode=query" &
                                "&scope=XboxLive.signin offline_access"

        Process.Start(New ProcessStartInfo(authUrl) With {
            .UseShellExecute = True
        })
    End Sub

    ''' <summary>
    ''' 等待用户授权并获取授权码
    ''' </summary>
    Private Sub WaitForAuthorizationCode()
        Dim context As HttpListenerContext = _listener.GetContext()
        Dim queryParams As NameValueCollection = HttpUtility.ParseQueryString(context.Request.Url.Query)
        _code = queryParams("code")

        If String.IsNullOrEmpty(_code) Then
            Throw New Exception("Code is not reachable")
        End If
    End Sub

    ''' <summary>
    ''' 返回成功页面
    ''' </summary>
    Private Sub SendSuccessResponse()
        Dim response As String = "<html><body><h1>Login is complete, please close this page and wait for program guidance</h1></body></html>"
        Dim buffer As Byte() = System.Text.Encoding.UTF8.GetBytes(response)

        ' 获取当前上下文
        Dim context As HttpListenerContext = _listener.GetContext()

        ' 使用 Using 语句管理 OutputStream
        Using outputStream = context.Response.OutputStream
            context.Response.ContentLength64 = buffer.Length
            outputStream.Write(buffer, 0, buffer.Length)
        End Using
    End Sub

    ''' <summary>
    ''' 获取授权码
    ''' </summary>
    Public Shared Function GetResponseData() As String
        If String.IsNullOrEmpty(_code) Then
            Throw New Exception("Code is not yet initialized")
        End If
        Return _code
    End Function

    ''' <summary>
    ''' 释放资源
    ''' </summary>
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _disposed Then
            If disposing Then
                ' 释放托管资源
                If _listener IsNot Nothing Then
                    _listener.Stop()
                    _listener.Close()
                End If
            End If
            _disposed = True
        End If
    End Sub

    ''' <summary>
    ''' 实现 IDisposable 接口
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

    ''' <summary>
    ''' 析构函数
    ''' </summary>
    Protected Overrides Sub Finalize()
        Dispose(disposing:=False)
    End Sub
End Class