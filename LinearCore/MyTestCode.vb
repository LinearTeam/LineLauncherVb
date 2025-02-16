Module Module1
    Sub Main()
        Using reg As New MicrosoftOAuthenticator()
            reg.Login()
        End Using
    End Sub
End Module