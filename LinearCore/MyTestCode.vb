Imports System.IO

Module Module1
    Sub Main()
        'Dim myForge As New OfficialForgeIndexProvider()
        'Dim forges = myForge.GetForgeForSpecificVersion("1.18.2").GetAwaiter().GetResult()
        'Dim url As String = ""
        'For Each forge In forges
        'If forge.isRecommended = True Then
        'url = forge.url
        'Exit For
        'End If
        'Next
        Dim downloader As New GameDownloader("Mojang")
        'Dim tempPath = ".\temp\forge\LatestForgeInstaller.jar"
        downloader.DownloadGameAsync(".\.minecraft", "1.18.2").GetAwaiter().GetResult()
        'downloader.DownloadForgeInstaller(tempPath, url).GetAwaiter().GetResult()
        'Dim vanillaJson = File.ReadAllText("F:\LineLauncherVb\LinearCore\bin\Debug\net8.0\.minecraft\versions\1.18.2\1.18.2.json")
        'Dim installerJson = OfficialForgeIndexProvider.GetInstallerJson(tempPath)
        'Dim returns = OfficialForgeIndexProvider.Combine(vanillaJson, installerJson)
        'File.WriteAllText("F:\LineLauncherVb\LinearCore\bin\Debug\net8.0\.minecraft\versions\1.18.2\1.18.2.json", JsonUtils.Serialize(returns.combinedJson))
        'Dim forgeFiles = returns.files
        'downloader.DownloadModLoaderFiles("F:\LineLauncherVb\LinearCore\bin\Debug\net8.0\.minecraft", forgeFiles).GetAwaiter().GetResult()
        Dim launcher As New GameInstanceLauncher() With {
            .root = "E:\LineLauncherVb\LinearCore\bin\Debug\net8.0\.minecraft",
            .memory = New MemoryCtrl() With {
                .max = 4096,
                .min = 128
            },
            .version = "1.18.2",
            .java = "E:\Java22\bin\java.exe",
            .username = "SunXiaochuan"
        }
        launcher.LaunchGame()
    End Sub

End Module