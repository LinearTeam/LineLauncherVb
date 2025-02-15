Imports System.Text.Json.Nodes

Public Class MicrosoftOAuthenticator
    Private ReadOnly _poster As New HttpUtils()

    Private _code As String
    Private _accessToken As String
    Private _uhs As String
    Private _XBLToken As String
    Private _XSTSToken As String
    Private _jwt As String

    Private Const comsumer = "consumers" '致敬传奇 comsumers 和 consumer

    ''' <summary>
    ''' 登录
    ''' </summary>
    Public Sub Login()
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

        VerifyXBL()
        VerifyXSTS()
        VerifyMc()
        TryGetMcArchive()
    End Sub

    Private Sub VerifyXBL()
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

    Private Sub VerifyXSTS()
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

    Private Sub VerifyMc()
        Dim url = "https://api.minecraftservices.com/authentication/login_with_xbox"
        Dim data = <string>{
            "identityToken": "XBL3.0 x=uhs;xstsToken"
        }</string>.Value.Replace("uhs", _uhs).Replace("xstsToken", _XSTSToken)
        Dim response = _poster.PostWithJson(data, url, "application/json", "application/json").Result
        _jwt = JsonUtils.GetValueFromJson(response, "access_token")
    End Sub

    Private Function TryGetMcArchive(Optional isGetInfo As Boolean = False)
        Dim url = "https://api.minecraftservices.com/minecraft/profile"
        Dim header = <string>{
            "Authorization": "Bearer jwt"
        }</string>.Value.Replace("jwt", _jwt)
        Dim response = _poster.GetWithAuth($"Bearer {_jwt}", url, "application/json").Result
        If response.Contains("errorType") Then
            If JsonUtils.GetValueFromJson(response, "errorType") = "NOT_FOUND" Then
                Throw New Exception("The account is not available.")
            End If
        End If

        If isGetInfo Then
            Dim info As New Dictionary(Of String, String) From {
                {"name", JsonUtils.GetValueFromJson(response, "name")},
                {"uuid", JsonUtils.GetValueFromJson(response, "id")},
                {"skinUrl", JsonUtils.GetValueFromJson(response, "skins[0].url")}
            }
            Return info
        End If
        Return True
    End Function
End Class