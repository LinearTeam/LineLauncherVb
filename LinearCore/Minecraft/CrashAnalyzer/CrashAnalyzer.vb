Imports System.Dynamic
Imports System.IO
Imports System.Reflection

Public Class CrashAnalyzer
    Public report As New CrashReport()
    Private crashCases

    Public Sub New()
        Dim assembly As Assembly = Assembly.GetExecutingAssembly()
        Using stream As Stream = assembly.GetManifestResourceStream("LinearCore.Crashes.json")
            Using reader As New StreamReader(stream)
                crashCases = JsonUtils.Parse(reader.ReadToEnd())
            End Using
        End Using
    End Sub

    Public Function Analyze(log As String, language As ReportLanguages) As CrashReport
        Dim logLines = log.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        report.title = logLines(1).Replace("// ", "")
        For Each line In logLines
            If line.Contains("Time: ") Then
                report.time = line.Split("Time: ")(1)
            End If
            If line.Contains("Description: ") Then
                report.summary = line.Split("Description: ")(1)
            End If
        Next
        Select Case language
            Case ReportLanguages.English

        End Select
        Return report
    End Function
End Class