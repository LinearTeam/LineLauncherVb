Public Class StringUtils
    Public Shared Function WithQuote(content As String)
        Return Chr(34) + content + Chr(34)
    End Function

    Public Shared Function WithBrace(content As String)
        Return Chr(123) + content + Chr(125)
    End Function
End Class
