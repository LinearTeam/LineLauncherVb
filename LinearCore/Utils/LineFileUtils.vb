Imports System.IO
Imports System.Text.RegularExpressions

Public Class LineFileUtils
    ' |Key|:|Value|
    Private Const _Pattern As String = "\|(?<key>[^|]+)\|:\|(?<value>[^|]+)\|"
    Public Shared Function GetKeySet(ByVal path As String, ByVal section As String) As List(Of String)
        Dim res As New List(Of String)()
        If Not File.Exists(path) Then
            Directory.CreateDirectory(Directory.GetParent(path).FullName)
            File.Create(path).Close()
            Return res
        End If

        Dim startTag As String = $"|{section}|_start"
        Dim endTag As String = $"|{section}|_end"
        Dim insection As Boolean = False
        Dim lines = File.ReadAllLines(path)

        For Each line In lines
            If line.Trim() = startTag Then
                insection = True
                Continue For
            End If
            If line.Trim() = endTag Then
                insection = False
                Continue For
            End If

            If insection Then
                Dim match = Regex.Match(line, _Pattern)
                If match.Success AndAlso Not String.IsNullOrEmpty(match.Groups("key").Value) Then
                    res.Add(match.Groups("key").Value)
                End If
            End If
        Next

        Return res
    End Function

    Public Shared Function GetSections(ByVal path As String) As List(Of String)
        Dim lines = File.ReadAllLines(path)
        Dim res As New List(Of String)()
        Dim section As String = Nothing

        For Each line In lines
            If line.StartsWith("|"c) AndAlso line.EndsWith("|_start") Then
                section = line.Substring(1).Replace("|_start", "")
            End If
            If line.StartsWith("|"c) AndAlso line.EndsWith("|_end") Then
                If line.Substring(1).Replace("|_end", "").Equals(section) Then
                    res.Add(section)
                    section = Nothing
                End If
            End If
        Next

        Return res
    End Function

    Public Shared Sub DeleteSection(ByVal path As String, ByVal section As String)
        Dim lines = File.ReadAllLines(path)
        Dim totalLines(lines.Length - 1) As String
        Dim inSection As Boolean = False
        Dim i As Integer = 0

        For Each line In lines
            If Not (Not line.StartsWith("|"c) OrElse Not line.EndsWith("|_start")) Then
                If line.Substring(1).Replace("|_start", "") = section Then
                    inSection = True
                    Continue For
                End If
            End If
            If line.StartsWith("|"c) AndAlso line.EndsWith("|_end") Then
                If line.Substring(1).Replace("|_end", "").Equals(section) Then
                    inSection = False
                    Continue For
                End If
            End If
            If inSection Then
                Continue For
            End If
            totalLines(i) = line
            i += 1
        Next

        Dim length As Integer = 1
        For Each line In totalLines
            If String.IsNullOrEmpty(line) Then Continue For
            length += 1
        Next

        Dim reallyTotalLines(length - 1) As String
        i = 0
        For Each line In totalLines
            If String.IsNullOrEmpty(line) Then Continue For
            reallyTotalLines(i) = line
            i += 1
        Next

        File.WriteAllLines(path, reallyTotalLines)
    End Sub

    Public Shared Function Read(ByVal path As String, ByVal key As String, ByVal section As String) As String
        If Not File.Exists(path) Then
            Directory.CreateDirectory(Directory.GetParent(path).FullName)
            File.Create(path).Close()
            Return Nothing
        End If

        Dim startTag As String = $"|{section}|_start"
        Dim endTag As String = $"|{section}|_end"
        Dim insection As Boolean = False
        Dim keyValue As String = Nothing

        Dim lines = File.ReadAllLines(path)

        For Each line In lines
            If line.Trim() = startTag Then
                insection = True
                Continue For
            End If
            If line.Trim() = endTag Then
                insection = False
                Continue For
            End If

            If insection Then
                Dim match = Regex.Match(line, _Pattern)
                If match.Success AndAlso match.Groups("key").Value = key Then
                    keyValue = match.Groups("value").Value
                    Exit For
                End If
            End If
        Next

        Return keyValue
    End Function

    Public Shared Sub Write(ByVal path As String, ByVal key As String, ByVal value As String, ByVal section As String)
        If value.Contains("|"c) OrElse key.Contains("|"c) OrElse section.Contains("|"c) Then
            Throw New Exception("key/value/section contains '|'")
        End If

        If Not File.Exists(path) Then
            Directory.CreateDirectory(Directory.GetParent(path).FullName)
            File.Create(path).Close()
        End If

        Dim startTag As String = $"|{section}|_start"
        Dim endTag As String = $"|{section}|_end"
        Dim insection As Boolean = False
        Dim sectionFound As Boolean = False
        Dim keyFound As Boolean = False
        Dim lines = File.ReadAllLines(path).ToList()

        For i As Integer = 0 To lines.Count - 1
            If lines(i).Trim() = startTag Then
                insection = True
                sectionFound = True
                Continue For
            End If
            If lines(i).Trim() = endTag Then
                insection = False
                If Not keyFound Then
                    lines.Insert(i, $"|{key}|:|{value}|")
                    keyFound = True
                End If
                Continue For
            End If

            If insection Then
                Dim match = Regex.Match(lines(i), _Pattern)
                If match.Success AndAlso match.Groups("key").Value = key Then
                    lines(i) = $"|{key}|:|{value}|"
                    keyFound = True
                End If
            End If
        Next

        If Not sectionFound Then
            lines.Add(startTag)
            lines.Add($"|{key}|:|{value}|")
            lines.Add(endTag)
        End If

        File.WriteAllLines(path, lines)
    End Sub
End Class