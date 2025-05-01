Public Class NetworkException
    Inherits Exception
    Public Sub New()
        MyBase.New("A network exception occured, please check your network connection.")
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class

Public Class VersionNotFoundException
    Inherits Exception
    Public Sub New()
        MyBase.New("Version not found.")
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class

Public Class UndefinedMirrorException
    Inherits Exception
    Public Sub New()
        MyBase.New("Undefined or incompleted mirror")
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class