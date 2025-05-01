Public Class GeneralParameters
    Private Const _quote = Chr(34)
    Public literal As String

    Public Sub AddPair(type As String, key As String, value As String)
        Select Case type
            Case "jvm"
                literal += $"-{value} "
            Case "mc"
                literal += $"--{key} {_quote}{value}{_quote} "
            Case "D"
                literal += $"-{key}={_quote}{value}{_quote} "
            Case "mainClass"
                literal += $"{value} "
            Case "cp"
                literal += $"-cp {value} "
            Case "java"
                literal += $"{Chr(34)}{value}{Chr(34)} "
        End Select
    End Sub

    Public Overrides Function ToString() As String
        Return literal
    End Function
End Class

Public Class ClassPathParameters
    Private literal As String = Chr(34)
    Private isAllowToAdd = True
    Private isClosed = False
    Public Sub AddItem(newPath As String)
        If isAllowToAdd Then
            literal += newPath + ";"
            Return
        End If
        Throw New Exception("The cp field was closed.")
    End Sub
    Public Overrides Function ToString() As String
        If isClosed Then
            Return literal.ToString()
        Else
            Throw New Exception("The cp field is not yet closed.")
        End If
    End Function

    Public Sub Close()
        literal += Chr(34)
        literal = Left(literal, Len(literal) - 2) & Right(literal, 1)
        literal = literal.Replace("/", "\")
        isClosed = True
        isAllowToAdd = False
    End Sub
End Class