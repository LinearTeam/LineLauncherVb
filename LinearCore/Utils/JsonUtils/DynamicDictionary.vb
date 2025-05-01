Imports System.Dynamic
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Public Class DynamicDictionary
    Inherits DynamicObject
    Implements IDictionary(Of String, Object)

    Private ReadOnly _dictionary As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase)

    Public Overrides Function TryGetMember(binder As GetMemberBinder, ByRef result As Object) As Boolean
        ' 始终返回True，不存在的键返回Nothing
        _dictionary.TryGetValue(binder.Name, result)
        Return True
    End Function

    Public Overrides Function TrySetMember(binder As SetMemberBinder, value As Object) As Boolean
        _dictionary(binder.Name) = value
        Return True
    End Function

    Public Function GetNestedKeys(Optional path As String = Nothing,
                            Optional recursive As Boolean = False) As IEnumerable(Of String)
        ' 根据路径定位到目标嵌套对象
        Dim targetDict As DynamicDictionary = Me
        If Not String.IsNullOrEmpty(path) Then
            If Not TryGetNestedDictionary(path, targetDict) Then
                Return Enumerable.Empty(Of String)()
            End If
        End If

        ' 获取键集合（递归或非递归）
        If recursive Then
            Return targetDict.GetAllKeys().Select(Function(kvp) kvp.Key)
        Else
            Return targetDict._dictionary.Keys
        End If
    End Function

    Public Iterator Function GetAllKeys(Optional prefix As String = "") As IEnumerable(Of KeyValuePair(Of String, Object))
        For Each kvp In _dictionary
            Dim fullKey = If(String.IsNullOrEmpty(prefix), kvp.Key, $"{prefix}.{kvp.Key}")

            If TypeOf kvp.Value Is DynamicDictionary Then
                ' 递归遍历嵌套对象
                For Each nestedKvp In DirectCast(kvp.Value, DynamicDictionary).GetAllKeys(fullKey)
                    Yield nestedKvp
                Next
            ElseIf TypeOf kvp.Value Is IList Then
                ' 处理数组/列表（可选）
                Yield New KeyValuePair(Of String, Object)(fullKey, kvp.Value)
            Else
                Yield New KeyValuePair(Of String, Object)(fullKey, kvp.Value)
            End If
        Next
    End Function

    Private Function TryGetNestedDictionary(path As String, ByRef result As DynamicDictionary) As Boolean
        Dim keys = path.Split("."c)
        Dim current As Object = Me

        For Each key In keys
            If TypeOf current Is DynamicDictionary Then
                Dim dict = DirectCast(current, DynamicDictionary)
                If Not dict._dictionary.TryGetValue(key, current) Then
                    result = Nothing
                    Return False
                End If
            Else
                result = Nothing
                Return False
            End If
        Next

        If TypeOf current Is DynamicDictionary Then
            result = DirectCast(current, DynamicDictionary)
            Return True
        End If

        result = Nothing
        Return False
    End Function

    Default Public Property Item(key As String) As Object Implements IDictionary(Of String, Object).Item
        Get
            Return _dictionary(key)
        End Get
        Set(value As Object)
            _dictionary(key) = value
        End Set
    End Property

    Public ReadOnly Property Keys As ICollection(Of String) Implements IDictionary(Of String, Object).Keys
        Get
            Return _dictionary.Keys
        End Get
    End Property

    Public ReadOnly Property Values As ICollection(Of Object) Implements IDictionary(Of String, Object).Values
        Get
            Return _dictionary.Values
        End Get
    End Property

    Public ReadOnly Property Count As Integer Implements ICollection(Of KeyValuePair(Of String, Object)).Count
        Get
            Return _dictionary.Count
        End Get
    End Property

    Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of KeyValuePair(Of String, Object)).IsReadOnly
        Get
            Return False
        End Get
    End Property

    Public Sub Add(key As String, value As Object) Implements IDictionary(Of String, Object).Add
        _dictionary.Add(key, value)
    End Sub

    Public Sub Add(item As KeyValuePair(Of String, Object)) Implements ICollection(Of KeyValuePair(Of String, Object)).Add
        _dictionary.Add(item.Key, item.Value)
    End Sub

    Public Sub Clear() Implements ICollection(Of KeyValuePair(Of String, Object)).Clear
        _dictionary.Clear()
    End Sub

    Public Function Contains(item As KeyValuePair(Of String, Object)) As Boolean Implements ICollection(Of KeyValuePair(Of String, Object)).Contains
        Return _dictionary.Contains(item)
    End Function

    Public Function ContainsKey(key As String) As Boolean Implements IDictionary(Of String, Object).ContainsKey
        Return _dictionary.ContainsKey(key)
    End Function

    Public Sub CopyTo(array() As KeyValuePair(Of String, Object), arrayIndex As Integer) Implements ICollection(Of KeyValuePair(Of String, Object)).CopyTo
        ' 更安全的实现方式
        ArgumentNullException.ThrowIfNull(array)
        If arrayIndex < 0 OrElse arrayIndex > array.Length Then
            Throw New ArgumentOutOfRangeException(NameOf(arrayIndex))
        End If
        If array.Length - arrayIndex < _dictionary.Count Then
            Throw New ArgumentException("目标数组空间不足")
        End If

        Dim i As Integer = arrayIndex
        For Each kvp In _dictionary
            array(i) = kvp
            i += 1
        Next
    End Sub

    Public Function GetEnumerator() As IEnumerator(Of KeyValuePair(Of String, Object)) Implements IEnumerable(Of KeyValuePair(Of String, Object)).GetEnumerator
        Return _dictionary.GetEnumerator()
    End Function

    Public Function Remove(key As String) As Boolean Implements IDictionary(Of String, Object).Remove
        Return _dictionary.Remove(key)
    End Function

    Public Function Remove(item As KeyValuePair(Of String, Object)) As Boolean Implements ICollection(Of KeyValuePair(Of String, Object)).Remove
        Return _dictionary.Remove(item.Key)
    End Function

    Public Function TryGetValue(key As String, ByRef value As Object) As Boolean Implements IDictionary(Of String, Object).TryGetValue
        Return _dictionary.TryGetValue(key, value)
    End Function

    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return _dictionary.GetEnumerator()
    End Function

    Public Overrides Function ToString() As String
        Return String.Join(", ", _dictionary.Select(Function(kvp) $"{kvp.Key}:{kvp.Value}"))
    End Function
End Class