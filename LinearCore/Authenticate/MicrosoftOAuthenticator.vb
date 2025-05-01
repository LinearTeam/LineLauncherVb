Public Class MicrosoftOAuthenticator
    Implements IDisposable

    Private ReadOnly _httpUtils As New HttpUtils()

    Private _code As String
    Private _accessToken As String
    Private _uhs As String
    Private _xblToken As String
    Private _xstsToken As String
    Private _jwt As String

    Private Const Consumer As String = "consumers" ' 致敬传奇 comsumers 和 consumer

    Public accountInfo As Dictionary(Of String, String)

    Private _disposed As Boolean = False

    ''' <summary>
    ''' 登录并获取 Minecraft 账户信息
    ''' </summary>
    Public Sub Login()
        Drop()
        Try
            ' 启动本地服务器并获取授权码
            Using server As New LocalServerProvider()
                server.StartServer()
                _code = LocalServerProvider.GetResponseData()
            End Using

            ' 获取 Access Token
            _accessToken = GetAccessToken(_code)

            ' 验证 Xbox Live 身份
            VerifyXBL()

            ' 验证 XSTS 身份
            VerifyXSTS()

            ' 验证 Minecraft 身份
            VerifyMinecraft()

            ' 获取 Minecraft 账户信息
            Dim profileInfo = TryGetMinecraftProfile()
            accountInfo = profileInfo
        Catch ex As Exception
            Console.WriteLine($"登录失败: {ex.Message}")
            Throw
        End Try
    End Sub

    ''' <summary>
    ''' 借助已有的 RefreshToken 登入并获取信息
    ''' </summary>
    Public Sub Refresh(refreshToken As String)
        Drop()
        Try
            ' 获取 Access Token
            _accessToken = GetAccessTokenByRefresh(refreshToken)

            ' 验证 Xbox Live 身份
            VerifyXBL()

            ' 验证 XSTS 身份
            VerifyXSTS()

            ' 验证 Minecraft 身份
            VerifyMinecraft()

            ' 获取 Minecraft 账户信息
            Dim profileInfo = TryGetMinecraftProfile()
            accountInfo = profileInfo
        Catch ex As Exception
            Console.WriteLine($"登录失败: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' 通过 RefreshToken 获取 AccessToken
    ''' </summary>
    ''' <param name="refreshToken">刷新令牌</param>
    ''' <returns></returns>
    Public Function GetAccessTokenByRefresh(refreshToken As String) As String
        Dim tokenRequestData As New Dictionary(Of String, String) From {
            {"client_id", "1cbfda79-fc84-47f9-8110-f924da9841ec"},
            {"scope", "XboxLive.signin offline_access"},
            {"refresh_token", refreshToken},
            {"grant_type", "refresh_token"}
        }
        Dim tokenUrl As String = $"https://login.microsoftonline.com/{Consumer}/oauth2/v2.0/token"
        Dim tokenResponse = _httpUtils.PostWithParametersAsync(tokenRequestData, tokenUrl, "application/json", "application/x-www-form-urlencoded").Result
        Dim tokenResult = JsonUtils.Parse(tokenResponse)

        Return tokenResult.access_token
    End Function

    ''' <summary>
    ''' 通过 code 获取 Access Token
    ''' </summary>
    Private Function GetAccessToken(code As String) As String
        Dim tokenRequestData As New Dictionary(Of String, String) From {
            {"client_id", "1cbfda79-fc84-47f9-8110-f924da9841ec"},
            {"scope", "XboxLive.signin offline_access"},
            {"code", code},
            {"redirect_uri", "http://127.0.0.1:40935"},
            {"grant_type", "authorization_code"}
        }

        Dim tokenUrl As String = $"https://login.microsoftonline.com/{Consumer}/oauth2/v2.0/token"
        Dim tokenResponse = _httpUtils.PostWithParametersAsync(tokenRequestData, tokenUrl, "application/json", "application/x-www-form-urlencoded").Result
        Dim tokenResult = JsonUtils.Parse(tokenResponse)

        Return tokenResult.access_token
    End Function

    ''' <summary>
    ''' 验证 Xbox Live 身份
    ''' </summary>
    Private Sub VerifyXBL()
        Dim xblUrl As String = "https://user.auth.xboxlive.com/user/authenticate"
        Dim xblData As String = <string>{
            "Properties": {
                "AuthMethod": "RPS",
                "SiteName": "user.auth.xboxlive.com",
                "RpsTicket": "d=accessToken"
            },
            "RelyingParty": "http://auth.xboxlive.com",
            "TokenType": "JWT"
        }</string>.Value.Replace("accessToken", _accessToken)

        Dim xblResponse = _httpUtils.PostWithJsonAsync(xblData, xblUrl, "application/json", "application/x-www-form-urlencoded").Result
        Dim parsedXblResponse = JsonUtils.Parse(xblResponse)

        _uhs = parsedXblResponse.DisplayClaims.xui(0).uhs
        _xblToken = parsedXblResponse.Token
    End Sub

    ''' <summary>
    ''' 验证 XSTS 身份
    ''' </summary>
    Private Sub VerifyXSTS()
        Dim xstsUrl As String = "https://xsts.auth.xboxlive.com/xsts/authorize"
        Dim xstsData As String = <string>{
            "Properties": {
                "SandboxId": "RETAIL",
                "UserTokens": [
                    "xblToken"
                ]
            },
            "RelyingParty": "rp://api.minecraftservices.com/",
            "TokenType": "JWT"
        }</string>.Value.Replace("xblToken", _xblToken)

        Dim xstsResponse = _httpUtils.PostWithJsonAsync(xstsData, xstsUrl, "application/json", "application/x-www-form-urlencoded").Result
        Dim parsedXstsResponse = JsonUtils.Parse(xstsResponse)
        _xstsToken = parsedXstsResponse.Token
    End Sub

    ''' <summary>
    ''' 验证 Minecraft 身份
    ''' </summary>
    Private Sub VerifyMinecraft()
        Dim mcUrl As String = "https://api.minecraftservices.com/authentication/login_with_xbox"
        Dim mcData As String = <string>{
            "identityToken": "XBL3.0 x=uhs;xstsToken"
        }</string>.Value.Replace("uhs", _uhs).Replace("xstsToken", _xstsToken)

        Dim mcResponse = _httpUtils.PostWithJsonAsync(mcData, mcUrl, "application/json", "application/json").Result
        Dim parsedMcResponse = JsonUtils.Parse(mcResponse)
        _jwt = parsedMcResponse.access_token
    End Sub

    ''' <summary>
    ''' 获取 Minecraft 账户信息
    ''' </summary>
    Private Function TryGetMinecraftProfile() As Dictionary(Of String, String)
        Dim profileUrl As String = "https://api.minecraftservices.com/minecraft/profile"
        Dim profileResponse = _httpUtils.GetWithAuthAsync($"Bearer {_jwt}", profileUrl, "application/json").Result
        Dim parsedProfile = JsonUtils.Parse(profileResponse)

        If parsedProfile.errorType IsNot Nothing Then
            If parsedProfile.errorType = "NOT_FOUND" Then
                Throw New Exception("The account is not available.")
            End If
        End If

        Dim skinUrl As String = Nothing
        If parsedProfile.skins IsNot Nothing AndAlso parsedProfile.skins.Count > 0 Then
            skinUrl = parsedProfile.skins(0).url
        End If

        Return New Dictionary(Of String, String) From {
            {"name", parsedProfile.name},
            {"uuid", parsedProfile.id},
            {"skinUrl", skinUrl}
        }
    End Function

    ''' <summary>
    ''' 释放资源
    ''' </summary>
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not _disposed Then
            If disposing Then
                ' 释放托管资源
                _httpUtils?.Dispose()
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

    ''' <summary>
    ''' 清空字段
    ''' </summary>
    Private Sub Drop()
        _code = Nothing
        _accessToken = Nothing
        _uhs = Nothing
        _xblToken = Nothing
        _xstsToken = Nothing
        _jwt = Nothing
        accountInfo = Nothing
    End Sub
End Class