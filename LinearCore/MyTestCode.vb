Module Module1
    Sub Main()
        Dim m As New MemoryCtrl With {
            .max = 4096,
            .min = 128
        }

        Dim a = New GameInstanceLauncher With {
            .root = "D:\LMC\.minecraft",
            .username = "Junse",
            .version = "1.12.2",
            .windowHeight = 600,
            .windowWidth = 800,
            .accessToken = "",
            .memory = m,
            .java = "E:\Java\bin\java.exe"
        }
        a.LaunchGame()
    End Sub
End Module