Public Class MicrosoftOAuthenticator
    Private ReadOnly _poster As New HttpUtils()

    Private _refreshToken As String
    Private _code As String
    Private _accessToken As String
    Private _uhs As String
    Private _XBLToken As String
    Private _XSTSToken As String

    Private Const comsumer = "consumers" '致敬传奇 comsumers 和 consumer
    Public Sub LoginAuthenticate()
        Dim server As New LocalServerProvider()
        server.StartServer()
        _code = LocalServerProvider.GetResponseData()

        Dim data As New Dictionary(Of String, String) From {
            {"client_id", "1cbfda79-fc84-47f9-8110-f924da9841ec"},
            {"scope", "XboxLive.signin offline_access"},
            {"code", _code},
            {"redirect_uri", "http://127.0.0.1:40935"},
            {"grant_type", "authorization_code"}
        }

        Dim url = $"https://login.microsoftonline.com/{comsumer}/oauth2/v2.0/token"
        Dim response = _poster.PostWithParameters(data, url, "application/json", "application/x-www-form-urlencoded").Result
        Dim result = JsonUtils.Parse(response)

        Dim newRefreshToken = result.refresh_token
        _accessToken = result.access_token

        XBLVerification()
        XSTSVerification()
    End Sub

    Private Sub XBLVerification()
        Dim url = "https://user.auth.xboxlive.com/user/authenticate"
        Dim data = <string>{
        "Properties": {
            "AuthMethod": "RPS",
            "SiteName": "user.auth.xboxlive.com",
            "RpsTicket": "d=accessToken"
        },
        "RelyingParty": "http://auth.xboxlive.com",
        "TokenType": "JWT"
    }</string>.Value.Replace("accessToken", _accessToken)
        Dim response = _poster.PostWithJson(data, url, "application/json", "application/x-www-form-urlencoded").Result
        Dim parsedRes = JsonUtils.Parse(response)

        _uhs = parsedRes.DisplayClaims.xui(0).uhs
        _XBLToken = parsedRes.Token
    End Sub

    Private Sub XSTSVerification()
        Dim url = "https://xsts.auth.xboxlive.com/xsts/authorize"
        Dim data = <string>{
       "Properties": {
           "SandboxId": "RETAIL",
           "UserTokens": [
               "xblToken"
           ]
       },
       "RelyingParty": "rp://api.minecraftservices.com/",
       "TokenType": "JWT"
        }</string>.Value.Replace("xblToken", _XBLToken)

        Dim response = _poster.PostWithJson(data, url, "application/json", "application/x-www-form-urlencoded").Result
        Dim parsedRes = JsonUtils.Parse(response)
        _XSTSToken = parsedRes.Token
    End Sub

    Private Sub MCVerification()
        Dim url = "https://api.minecraftservices.com/authentication/login_with_xbox"
        Dim data = <string>{
            "identityToken": "XBL3.0 x=uhs;xstsToken"
        }</string>.Value.Replace("uhs", _uhs).Replace("xstsToken", _XSTSToken)
        Dim response = _poster.PostWithJson(data, url, "application/json", "application/x-www-form-urlencoded")
        'TODO: Check if the user has purchased the game
    End Sub
End Class