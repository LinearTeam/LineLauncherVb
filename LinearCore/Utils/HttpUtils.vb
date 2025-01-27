Imports System.Net.Http
Imports System.Net.Http.Headers

Public Class HttpUtils
    Private ReadOnly _httpClient As HttpClient

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
    Public Async Function PostWithJson(json As String, url As String, accept As String, contentType As String) As Task(Of String)
        _httpClient.DefaultRequestHeaders.Accept.Clear()
        _httpClient.DefaultRequestHeaders.Accept.Add(New MediaTypeWithQualityHeaderValue(accept))
        Dim content = New StringContent(json)

        content.Headers.ContentType = New MediaTypeHeaderValue(contentType)

        Dim response = Await _httpClient.PostAsync(url, content)
        response.EnsureSuccessStatusCode()

        Dim responseContent = Await response.Content.ReadAsStringAsync()
        Return responseContent
    End Function


End Class
