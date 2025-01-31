Imports LinearCore.StringUtils
Public Class MicrosoftOAuthenticator
    Private ReadOnly _refreshToken As String
    Private Const comsumer = "consumers" '致敬传奇 comsumers 和 consumer
    Private _code As String
    Private _poster As New HttpUtils()
    Private _accessToken As String
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
        _accessToken = JsonUtils.GetValueFromJson(response, "access_token")
        Dim newRefreshToken = JsonUtils.GetValueFromJson(response, "refresh_token")

        XBLVerification()
    End Sub

    Private Sub XBLVerification()
        Dim url = "https://user.auth.xboxlive.com/user/authenticate"
        Dim data = $"{WithBrace(
        $"{WithQuote("Properties")}: {WithBrace(
            $"{WithQuote("AuthMethod")}: {WithQuote("RPS")},
            {WithQuote("SiteName")}: {WithQuote("user.auth.xboxlive.com")},
            {WithQuote("RpsTicket")}: {WithQuote($"d={_accessToken}")}
        ")},
        {WithQuote("RelyingParty")}: {WithQuote("http://auth.xboxlive.com")},
        {WithQuote("TokenType")}: {WithQuote("JWT")}
        ")}"
        'TODO: XBLVerification
    End Sub
End Class
