Imports System.Collections.Specialized
Imports System.Net
Imports System.Web

Public Class LocalServerProvider
    Private Shared code As String
    Private listener As HttpListener

    Public Sub New()
        listener = New HttpListener()
    End Sub

    Public Sub StartServer()
        Dim url As String = "http://127.0.0.1:40935/"
        listener.Prefixes.Add(url)
        listener.Start()

        Dim authUrl As String = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize?client_id=1cbfda79-fc84-47f9-8110-f924da9841ec&response_type=code&redirect_uri=http://127.0.0.1:40935&response_mode=query&scope=XboxLive.signin offline_access"
        Process.Start(New ProcessStartInfo(authUrl) With {
            .UseShellExecute = True
        })

        Dim context As HttpListenerContext = listener.GetContext()
        Dim requestUrl As String = context.Request.Url.Query
        Dim queryParams As NameValueCollection = HttpUtility.ParseQueryString(requestUrl)
        code = queryParams("code")

        Dim response As String = "<html><body><h1>Login is complete, please close this page and wait for program guidance</h1></body></html>"
        Dim buffer As Byte() = System.Text.Encoding.GetEncoding("utf-8").GetBytes(response)

        context.Response.ContentLength64 = buffer.Length
        context.Response.OutputStream.Write(buffer, 0, buffer.Length)
        context.Response.OutputStream.Close()

        listener.Stop()
    End Sub

    Public Shared Function GetResponseData() As String
        Return code
    End Function
End Class