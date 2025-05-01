Imports System.Net.Http
Imports System.Net.Http.Headers

Public Class HttpUtils
    Implements IDisposable

    Private ReadOnly _httpClient As HttpClient
    Private _disposed As Boolean = False

    Public Sub New(Optional userAgent As String = "$LMC/VB (Mozilla/5.0)")
        _httpClient = New HttpClient()
        _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent)
    End Sub

    ''' <summary>
    ''' 异步向一个 url 发送 get 请求
    ''' </summary>
    ''' <param name="url">url地址</param>
    ''' <param name="accept"></param>
    ''' <returns>返回请求到的内容</returns>
    Public Async Function GetAsync(url As String, Optional accept As String = Nothing) As Task(Of String)
        _httpClient.DefaultRequestHeaders.Clear()
        If accept IsNot Nothing Then
            _httpClient.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue(accept))
        End If
        Dim response = Await _httpClient.GetAsync(url)
        response.EnsureSuccessStatusCode()
        Return Await response.Content.ReadAsStringAsync()
    End Function

    ''' <summary>
    ''' 异步向 url 发送 post 请求
    ''' </summary>
    ''' <param name="json">请求文本</param>
    ''' <param name="url">请求url</param>
    ''' <param name="accept">请求头</param>
    ''' <param name="contentType">内容类型</param>
    ''' <returns></returns>
    Public Async Function PostWithJsonAsync(json As String, url As String, accept As String, contentType As String) As Task(Of String)
        _httpClient.DefaultRequestHeaders.Accept.Clear()
        _httpClient.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue(accept))
        Dim content = New StringContent(json)

        content.Headers.ContentType = New MediaTypeHeaderValue(contentType)

        Dim response = Await _httpClient.PostAsync(url, content)
        response.EnsureSuccessStatusCode()

        Dim responseContent = Await response.Content.ReadAsStringAsync()
        Return responseContent
    End Function

    ''' <summary>
    ''' 异步向 url 发送 post 请求（带参数）
    ''' </summary>
    ''' <param name="parameters">参数字典</param>
    ''' <param name="url">请求url</param>
    ''' <param name="accept">请求头</param>
    ''' <param name="contentType">内容类型</param>
    ''' <returns></returns>
    Public Async Function PostWithParametersAsync(parameters As Dictionary(Of String, String), url As String, accept As String, contentType As String) As Task(Of String)
        _httpClient.DefaultRequestHeaders.Accept.Clear()
        _httpClient.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue(accept))

        Dim content = New FormUrlEncodedContent(parameters)
        content.Headers.ContentType = New MediaTypeHeaderValue(contentType)

        Dim response = Await _httpClient.PostAsync(url, content)
        Dim responseContent = Await response.Content.ReadAsStringAsync()
        Return responseContent
    End Function

    ''' <summary>
    ''' 异步向 url 发送带认证头的 get 请求
    ''' </summary>
    ''' <param name="auth">认证头</param>
    ''' <param name="url">请求url</param>
    ''' <param name="accept">请求头</param>
    ''' <returns></returns>
    Public Async Function GetWithAuthAsync(auth As String, url As String, accept As String) As Task(Of String)
        _httpClient.DefaultRequestHeaders.Clear()
        _httpClient.DefaultRequestHeaders.Add("Authorization", auth)
        _httpClient.DefaultRequestHeaders.Accept.Clear()
        _httpClient.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue(accept))
        Dim response = Await _httpClient.GetAsync(url)
        response.EnsureSuccessStatusCode()

        Dim responseContent = Await response.Content.ReadAsStringAsync()
        Return responseContent
    End Function

    Public Shared Async Function GrabWebSrcAsync(url As String, Optional timeoutSeconds As Integer = 30) As Task(Of String)
        Using client As New HttpClient()
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            Try
                Dim response As HttpResponseMessage = Await client.GetAsync(url)
                response.EnsureSuccessStatusCode()
                Return Await response.Content.ReadAsStringAsync()
            Catch ex As TaskCanceledException When ex.CancellationToken.IsCancellationRequested = False
                Return $"Request timed out after {timeoutSeconds} seconds"
            Catch ex As Exception
                Return $"Error: {ex.Message}"
            End Try
        End Using
    End Function

    ''' <summary>
    ''' 释放资源
    ''' </summary>
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _disposed Then
            If disposing Then
                ' 释放托管资源
                _httpClient?.Dispose()
            End If

            ' 释放非托管资源（如果有）
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
