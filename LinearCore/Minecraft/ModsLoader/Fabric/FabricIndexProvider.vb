Public Class FabricIndexProvider
    Private _fabricIndex As String

    Private ReadOnly _hostProvider As HostProvider
    Private ReadOnly _httpUtils As New HttpUtils()

    Public Sub New(mirrorType As String)
        _hostProvider = New HostProvider(mirrorType)
    End Sub

    Private Sub GetFabricIndex()
        _fabricIndex = _httpUtils.GetAsync(_hostProvider.provideMirror.fabric).Result
    End Sub
End Class
