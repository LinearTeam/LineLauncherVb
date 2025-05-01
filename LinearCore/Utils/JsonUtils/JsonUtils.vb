Imports System.Runtime.CompilerServices
Imports System.Text.Encodings.Web
Imports System.Text.Json
Class JsonUtils

    Public Shared Function Parse(jsonString As String) As Object
        Dim document = JsonDocument.Parse(jsonString)
        Try
            Return ProcessElement(document.RootElement)
        Finally
            document.Dispose()
        End Try
    End Function

    Private Shared Function ProcessElement(element As JsonElement) As Object
        Select Case element.ValueKind
            Case JsonValueKind.Object
                Return ProcessObject(element)
            Case JsonValueKind.Array
                Return ProcessArray(element)
            Case JsonValueKind.String
                Return element.GetString()
            Case JsonValueKind.Number
                Return element.GetDouble()
            Case JsonValueKind.True, JsonValueKind.False
                Return element.GetBoolean()
            Case JsonValueKind.Null
                Return Nothing
            Case Else
                Throw New NotSupportedException($"Unsupported JSON element type: {element.ValueKind}")
        End Select
    End Function

    Private Shared Function ProcessObject(element As JsonElement) As DynamicDictionary
        Dim dict = New DynamicDictionary()

        For Each propertyElement In element.EnumerateObject()
            dict(propertyElement.Name) = ProcessElement(propertyElement.Value)
        Next

        Return dict
    End Function

    Private Shared Function ProcessArray(element As JsonElement) As List(Of Object)
        Dim list = New List(Of Object)()

        For Each item In element.EnumerateArray()
            list.Add(ProcessElement(item))
        Next

        Return list
    End Function

    Public Shared Function Serialize(obj As Object) As String
        Return JsonSerializer.Serialize(obj, GetJsonSerializerOptions())
    End Function

    Private Shared Function GetJsonSerializerOptions() As JsonSerializerOptions
        Return New JsonSerializerOptions With {
            .PropertyNameCaseInsensitive = True,
            .Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            .WriteIndented = True
        }
    End Function
End Class