Imports System.IO
Imports System.IO.Compression
Imports System.Text

''' <summary>
''' 游戏实例启动器, 依次需要 游戏根目录 版本 用户名 java路径 内存 窗体高宽 access token, uuid（在线登录，可选）
''' </summary>
Public Class GameInstanceLauncher
    Public root, version, username, java As String
    Public memory As MemoryCtrl
    Public windowHeight, windowWidth As Integer
    Public accessToken, uuid As String
    Public extendedForgeJvmParam, extendedForgeGameParam As String

    Private Sub UnpressForDll(source As String)
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
        Dim cp As New ClassPathParameters()
        For Each i In indexes
            If Not i.isNative And Not i.isResources Then
                cp.AddItem(i.path) '加入 artifact
                Continue For
            End If

            If i.isClient Then
                cp.AddItem(i.path) '加入客户端 jar
            End If

            If i.isNative Then
                UnpressForDll(i.path) '解压 natives
            End If
        Next
        cp.Close()

        Dim tempVersionJson = JsonUtils.Parse(File.ReadAllText($"{root}\versions\{version}\{version}.json"))
        Dim mainClass = tempVersionJson.mainClass
        Dim indexId = tempVersionJson.assetIndex.id

        Dim commands As New GeneralParameters()

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
        Dim cps = New List(Of String)
        For Each path In cp.ToString().Split(";")
            If cps.Contains(path) Then
                Continue For
            End If
            cps.Add(path)
        Next
        Dim actualCp = String.Join(";", cps)
        commands.AddPair("cp", Nothing, actualCp)

        If extendedForgeJvmParam IsNot Nothing Then
            extendedForgeJvmParam = extendedForgeJvmParam.Replace("${library_directory}", $"{root}\libraries").Replace("${classpath_separator}", ";").Replace("${version_name}", version)
        End If

        'mc 参数
        commands.AddPair("mainClass", Nothing, mainClass)
        commands.AddPair("mc", "username", username)
        commands.AddPair("mc", "version", version)
        commands.AddPair("mc", "gameDir", $"{root}\versions\{version}")
        commands.AddPair("mc", "assetsDir", $"{root}\assets")
        commands.AddPair("mc", "assetIndex", indexId)

        If accessToken IsNot Nothing And uuid IsNot Nothing Then
            commands.AddPair("mc", "uuid", uuid)
            commands.AddPair("mc", "accessToken", accessToken)
        Else
            commands.AddPair("mc", "uuid", "00000000-0000-0000-0000-00000000000")
            commands.AddPair("mc", "accessToken", "1241258925")
        End If

        commands.AddPair("mc", "clientId", "")
        commands.AddPair("mc", "xuid", "")
        commands.AddPair("mc", "userType", "Legacy")
        commands.AddPair("mc", "versionType", "LMC")

        Dim commandArr = commands.ToString().Split($" {mainClass}")
        Dim commandString = commandArr(0) + extendedForgeJvmParam + " " + mainClass + commandArr(1) + " " + extendedForgeGameParam

        File.WriteAllText("./launch.bat", commandString) '写入到 bat
        Process.Start("./launch.bat") '执行
    End Sub
End Class