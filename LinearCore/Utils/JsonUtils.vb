Imports System.Dynamic
Imports System.Runtime.CompilerServices
Imports System.Text.Encodings.Web
Imports System.Text.Json
Imports System.Text.Json.Serialization

' 此模块容易误用
' 对于单次解析, 带有以格式化字符串形式的 JsonPath（非必须），使用 GetValueFromJson
' 对于重复解析，带有以格式化字符串形式的 JsonPath（非必须），新建 JsonUtils 实例后使用 GetNestedValue
' 对于重复解析，不带有以格式化字符串形式的 JsonPath（必须），使用 Parse 后以访问成员的形式访问 Json
' 在合适的情况下，允许换用，但是保证功能正常

''' <summary>
''' 动态 Json 解析器
''' </summary>
''' <returns> 返回一个解析器实例 </returns>
''' <remark> </remark >

Public Class JsonUtils
    Private root As JsonElement
    Private ReadOnly jsonDocument As JsonDocument
    Private Shared ReadOnly separator As Char() = New Char() {"."c}
    Private Shared ReadOnly separatorArray As Char() = New Char() {"["c, "]"c}

    Private Shared ReadOnly DefaultJsonOptions As New JsonSerializerOptions With {
        .WriteIndented = True,
        .PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        .DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        .Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    }

    ''' <param name="jsonString">
    ''' 字符串形式的 Json 内容
    ''' </param>
    Public Sub New(jsonString As String)
        jsonDocument = JsonDocument.Parse(jsonString)
        root = jsonDocument.RootElement
    End Sub

    Private Function GetValue(key As String) As JsonElement
        If root.TryGetProperty(key, root) Then
            Return root
        Else
            Throw New KeyNotFoundException($"Key '{key}' not found in JSON.")
        End If
    End Function

    ''' <param name="key">
    ''' 欲要访问属性的键
    ''' </param>
    ''' <returns>
    ''' 一个字符串类型的对象
    ''' </returns>
    Public Function GetString(key As String) As String
        Return GetValue(key).GetString()
    End Function

    ''' <param name="key">
    ''' 欲要访问属性的键
    ''' </param>
    ''' <returns>
    ''' 一个整数类型的对象
    ''' </returns>
    Public Function GetInt(key As String) As Integer
        Return GetValue(key).GetInt32()
    End Function

    ''' <param name="key">
    ''' 欲要访问属性的键
    ''' </param>
    ''' <returns>
    ''' 返回一个布尔值类型的对象
    ''' </returns>
    Public Function GetBoolean(key As String) As Boolean
        Return GetValue(key).GetBoolean()
    End Function

    ''' <param name="key">
    ''' 欲要访问属性的键
    ''' </param>
    ''' <returns>
    ''' 返回一个迭代器
    ''' </returns>
    Public Function GetArray(key As String) As JsonElement.ArrayEnumerator
        Return GetValue(key).EnumerateArray()
    End Function

    ''' <param name="nestedPropertyPath">
    ''' 欲要访问属性的路径，以 "." 分割，例如 product.info.id
    ''' </param>
    ''' <param name="splitChar">
    ''' 当 . 不可用时，自定义的分隔符
    ''' </param>
    ''' <returns>
    ''' 一个动态类型的对象
    ''' </returns>
    Public Function GetNestedValue(nestedPropertyPath As String, Optional splitChar As String = ".") As Object
        Dim properties() As String = nestedPropertyPath.Split(splitChar)
        Dim currentElement As JsonElement = root

        For Each prop In properties
            If currentElement.TryGetProperty(prop, currentElement) Then
                If currentElement.ValueKind = JsonValueKind.Object OrElse currentElement.ValueKind = JsonValueKind.Array Then
                    Continue For
                Else
                    Return GetValueFromElement(currentElement)
                End If
            Else
                Return Nothing
            End If
        Next

        Return Nothing
    End Function

    Private Shared Function GetValueFromElement(element As JsonElement) As Object
        Select Case element.ValueKind
            Case JsonValueKind.String
                Return element.GetString()
            Case JsonValueKind.Number
                Dim intValue As Integer
                If element.TryGetInt32(intValue) Then
                    Return intValue
                Else
                    Return element.GetDouble()
                End If
            Case JsonValueKind.True, JsonValueKind.False
                Return element.GetBoolean()
            Case JsonValueKind.Null
                Return Nothing
            Case JsonValueKind.Array
                Return element.EnumerateArray().Select(Function(e) GetValueFromElement(e)).ToList()
            Case JsonValueKind.Object
                Return element
            Case Else
                Return Nothing
        End Select
    End Function

    Public Shared Function GetValueFromJson(jsonString As String, path As String) As String
        Try
            Using document As JsonDocument = JsonDocument.Parse(jsonString)
                Dim keys As String() = path.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                Dim element As JsonElement = document.RootElement

                For Each key In keys
                    If key.Contains("["c) Then
                        Dim parts As String() = key.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries)
                        Dim arrayKey As String = parts(0)
                        Dim index As Integer = Integer.Parse(parts(1))

                        If element.TryGetProperty(arrayKey, element) AndAlso element.ValueKind = JsonValueKind.Array Then
                            element = element(index)
                        Else
                            Return Nothing
                        End If
                    Else
                        If element.TryGetProperty(key, element) Then
                        Else
                            Return Nothing
                        End If
                    End If
                Next

                Return element.ToString()
            End Using
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <param name="nestedPropertyPath">
    ''' 欲要访问属性的路径，以 "." 分割，例如 product.info.id
    ''' </param>
    ''' <returns>
    ''' 返回嵌套属性下的所有键的列表
    ''' </returns>
    Function GetNestedKeys(path As String) As List(Of String)
        Dim keys As New List(Of String)()

        Dim jsonElement As JsonElement = jsonDocument.RootElement

        ' 根据路径获取嵌套元素
        Dim properties As String() = path.Split("."c)
        For Each prop In properties
            If Not jsonElement.TryGetProperty(prop, jsonElement) Then
                Return keys ' 如果路径无效，返回空列表
            End If
        Next

        ' 检查是否为对象，并获取所有键
        If jsonElement.ValueKind = JsonValueKind.Object Then
            For Each prop In jsonElement.EnumerateObject()
                keys.Add(prop.Name)
            Next
        End If

        Return keys
    End Function


    Public Shared Function Parse(jsonString As String) As Object
        Dim document = JsonDocument.Parse(jsonString)
        Try
            Return ProcessElement(document.RootElement)
        Finally
            document.Dispose()
        End Try
    End Function

    Public Shared Function Serialize(expando As ExpandoObject) As String
        Return JsonSerializer.Serialize(ConvertExpandoToDictionary(expando), DefaultJsonOptions)
    End Function

    Private Shared Function ConvertExpandoToDictionary(expando As ExpandoObject) As Dictionary(Of String, Object)
        Dim dict = New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)

        For Each kvp In DirectCast(expando, IDictionary(Of String, Object))
            dict(kvp.Key) = ConvertExpandoValue(kvp.Value)
        Next

        Return dict
    End Function

    Private Shared Function ConvertExpandoValue(value As Object) As Object
        If value Is Nothing Then Return Nothing

        If TypeOf value Is ExpandoObject Then
            Return ConvertExpandoToDictionary(DirectCast(value, ExpandoObject))
        ElseIf TypeOf value Is List(Of Object) Then
            Return ConvertExpandoList(DirectCast(value, List(Of Object)))
        Else
            Return value
        End If
    End Function

    Private Shared Function ConvertExpandoList(list As List(Of Object)) As List(Of Object)
        Dim newList = New List(Of Object)()

        For Each item In list
            newList.Add(ConvertExpandoValue(item))
        Next

        Return newList
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

    Private Shared Function ProcessObject(element As JsonElement) As ExpandoObject
        Dim obj = New ExpandoObject()
        Dim dict = TryCast(obj, IDictionary(Of String, Object))

        For Each propertyElement In element.EnumerateObject()
            dict(propertyElement.Name) = ProcessElement(propertyElement.Value)
        Next

        Return obj
    End Function

    Private Shared Function ProcessArray(element As JsonElement) As List(Of Object)
        Dim list = New List(Of Object)()

        For Each item In element.EnumerateArray()
            list.Add(ProcessElement(item))
        Next

        Return list
    End Function

    Protected Overrides Sub Finalize()
        jsonDocument?.Dispose()
        MyBase.Finalize()
    End Sub

End Class