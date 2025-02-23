Imports System.IO
Imports System.IO.Compression
Imports System.Runtime.InteropServices
Imports System.Security.Cryptography.X509Certificates

Public Class MemoryCtrl
    Public min, max As String
End Class

Public Class GeneralArgs
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
                literal += $"{value} "
        End Select
    End Sub

    Public Overrides Function ToString() As String
        Return literal
    End Function
End Class

Public Class ClassPathArgument
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

''' <summary>
''' 游戏实例启动器, 依次需要 游戏根目录 版本 用户名 java路径 内存 窗体高宽 access token（在线登录，可选）
''' </summary>
Public Class GameInstanceLauncher
    Public root, version, username, java As String
    Public memory As MemoryCtrl
    Public windowHeight, windowWidth As Integer
    Public accessToken As String

    Private Sub Unpress(source As String)
        Dim targetFolder = $"{root}\versions\{version}\{version}-natives"
        Directory.CreateDirectory(targetFolder)
        Dim tempDir As String = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
        Try
            Directory.CreateDirectory(tempDir)
            ZipFile.ExtractToDirectory(source, tempDir)

            For Each dllFile In Directory.EnumerateFiles(tempDir, "*.dll", SearchOption.AllDirectories)
                Dim fileName As String = Path.GetFileName(dllFile)
                Dim targetPath As String = Path.Combine(targetFolder, fileName)

                File.Move(dllFile, targetPath)
                Console.WriteLine($"Unpressed {fileName}")
            Next
        Catch ex As Exception
            Console.WriteLine($"An error occurred during decompression: {source}. Error: {ex.Message}")
        End Try
    End Sub

    Public Sub LaunchGame()
        Dim indexes = GameIndexParser.ParseIndex(version, $"{root}\versions\{version}\{version}.json", root, "Mojang")
        Dim cp As New ClassPathArgument()
        For Each i In indexes
            If Not i.isNative And Not i.isResources Then
                cp.AddItem(i.path) '加入 artifact
                Continue For
            End If

            If i.isClient Then
                cp.AddItem(i.path) '加入客户端 jar
            End If

            If i.isNative Then
                Unpress(i.path) '解压 natives
            End If
        Next
        cp.Close()

        Dim mainClass = JsonUtils.GetValueFromJson(File.ReadAllText($"{root}\versions\{version}\{version}.json"), "mainClass")
        Dim indexId = JsonUtils.GetValueFromJson(File.ReadAllText($"{root}\versions\{version}\{version}.json"), "assetIndex.id")

        Dim commands As New GeneralArgs()

        'java
        commands.AddPair("java", Nothing, java)

        'jvm 参数
        commands.AddPair("jvm", Nothing, $"Xmx{memory.max}M")
        commands.AddPair("jvm", Nothing, $"Xmn{memory.min}M")
        commands.AddPair("jvm", Nothing, $"XX:+UseG1GC")
        commands.AddPair("jvm", Nothing, $"XX:-UseAdaptiveSizePolicy")
        commands.AddPair("jvm", Nothing, $"XX:-OmitStackTraceInFastThrow")

        '-D 参数
        commands.AddPair("D", "Dos.name", $"Windows 10")
        commands.AddPair("D", "Dos.version", $"10.0")
        commands.AddPair("D", "Dminecraft.launcher.brand", $"LMC")
        commands.AddPair("D", "Dminecraft.launcher.version", $"1.0.0")
        commands.AddPair("D", "Djava.library.path", $"{root}\versions\{version}\{version}-natives")

        'cp 参数
        commands.AddPair("cp", Nothing, cp.ToString())

        'mc 参数
        commands.AddPair("mainClass", Nothing, mainClass)
        commands.AddPair("mc", "username", username)
        commands.AddPair("mc", "version", version)
        commands.AddPair("mc", "gameDir", $"{root}\versions\{version}")
        commands.AddPair("mc", "assetsDir", $"{root}\assets")
        commands.AddPair("mc", "assetIndex", indexId)
        commands.AddPair("mc", "uuid", "00000000-0000-0000-0000-00000000000")
        commands.AddPair("mc", "accessToken", "1241258925")
        commands.AddPair("mc", "clientId", "")
        commands.AddPair("mc", "xuid", "")
        commands.AddPair("mc", "userType", "Legacy")
        commands.AddPair("mc", "versionType", "LMC")

        File.WriteAllText("./launch.bat", commands.ToString()) '写入到 bat
        Process.Start("./launch.bat") '执行
    End Sub
End Class