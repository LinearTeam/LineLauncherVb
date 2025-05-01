Imports System.IO
Imports System.IO.Compression
Imports System.Text.RegularExpressions

Public Class OfficialForgeIndexProvider
    Public versions As New List(Of String)
    Public Sub New()
        versions = FetchForgeEnabledVersionsAsync().GetAwaiter().GetResult()
    End Sub

    Public Shared Async Function FetchForgeEnabledVersionsAsync() As Task(Of List(Of String))
        Dim content = Await HttpUtils.GrabWebSrcAsync("https://files.minecraftforge.net/net/minecraftforge/forge/index_1.2.4.html")
        Dim pattern = "<a href=""index_([^""]+\.[^""]+\.[^""]+)\.html"">"
        Dim matchCollection = Regex.Matches(content, pattern)

        Dim versions As New List(Of String)
        For Each match As Match In matchCollection
            versions.Add(match.Groups(1).Value)
        Next
        Return versions
    End Function

    Public Async Function GetForgeForSpecificVersion(version As String) As Task(Of List(Of ForgeInfo))
        If version = "1.7.10-pre4" Then
            version = "1.7.10_pre4"
        End If

        If versions.Contains(version) Then
            Dim content = Await HttpUtils.GrabWebSrcAsync($"https://files.minecraftforge.net/net/minecraftforge/forge/index_{version}.html")
            Dim result = ParseSpecificForgeHtml(content)
            Return result
        Else
            Throw New Exception($"Forge is not available for {version}")
        End If
    End Function

    Private Shared Function ParseSpecificForgeHtml(html As String) As List(Of ForgeInfo)
        Dim lines = html.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        Dim forges As New List(Of ForgeInfo)
        Dim pattern = "https://maven\.minecraftforge\.net/net/minecraftforge/forge/([^-]+-[^/]+)/forge-\1-installer\.jar"
        Dim lineIndex = 0
        For Each line In lines
            Dim forge As New ForgeInfo()
            If Regex.IsMatch(line, pattern) Then '匹配含有 url 的行
                If Not line.Contains("Direct Download") Then '排除 Direct Download
                    If Not line.Contains("title") Then '排除 title
                        Dim actualUrl = line.TrimStart().Split("url=")(1) '配平删除前面的空格，截取 url
                        actualUrl = Left(actualUrl, Len(actualUrl) - 2) '去除其他无用符号
                        forge.url = actualUrl
                    End If
                End If
            End If
            If line.Contains("download-version") Then '匹配版本信息项
                Dim version = lines(lineIndex + 1).TrimStart() '配平
                forge.version = version
                If lines(lineIndex + 2).Contains("promo-latest") Then
                    forge.isLatest = True '检查是否是最新版
                End If
                If lines(lineIndex + 2).Contains("promo-recommended") Then
                    forge.isRecommended = True '检查是否是推荐版
                End If
            End If
            lineIndex += 1
            If line.Contains("download-time") Then
                Dim releaseDate = line.Split("title=")(1).Split(">")(0).Replace(Chr(34), "")
                forge.releaseDate = releaseDate '拿出发行日期
            End If
            forges.Add(forge)
        Next

        '由于先前步骤每个循环都创建一个 ForgeInfo 实例，所以信息不在一起，需要二次解析
        Dim tempVersion As String = ""
        Dim tempDate As String = ""
        Dim tempIsLatest = False
        Dim tempIsRecommended = False

        '按照顺序，先扫到 Forge 的版本和是否是最新版、推荐版信息，接着是日期，最后是 url
        For Each forge In forges
            If forge.version IsNot Nothing Then
                tempVersion = forge.version
                tempIsLatest = forge.isLatest
                tempIsRecommended = forge.isRecommended
            End If
            If forge.releaseDate IsNot Nothing Then
                tempDate = forge.releaseDate
            End If
            If forge.url IsNot Nothing Then
                forge.version = tempVersion
                forge.releaseDate = tempDate
                forge.isRecommended = tempIsRecommended
                forge.isLatest = tempIsLatest
            End If
        Next

        '经过二次解析后，所有信息已经被重新排序，删去没有 url 的空项
        '倒序遍历
        For index As Integer = forges.Count - 1 To 0 Step -1
            If forges(index).url Is Nothing Then
                forges.RemoveAt(index)
            End If
        Next
        Return forges
    End Function

    Private Shared Function GetVersionJsonFromJar(jarFilePath As String) As String
        ' 检查 JAR 文件是否存在
        If Not File.Exists(jarFilePath) Then
            Throw New FileNotFoundException("JAR文件未找到", jarFilePath)
        End If

        ' JAR文件实际上是ZIP格式，所以我们可以使用 ZipArchive
        Using archive As ZipArchive = ZipFile.OpenRead(jarFilePath)
            ' 查找 version.json 条目
            Dim versionEntry = archive.GetEntry("version.json")

            If versionEntry Is Nothing Then
                Throw New FileNotFoundException("version.json 未在 JAR 文件中找到")
            End If

            ' 读取文件内容
            Using reader As New StreamReader(versionEntry.Open())
                Return reader.ReadToEnd()
            End Using
        End Using
    End Function

    Public Shared Function GetInstallerJson(pathToInstaller As String)
        Dim installerJson = GetVersionJsonFromJar(pathToInstaller)
        Return installerJson
    End Function

    Public Shared Function Combine(vanillaJson As String, forgeJson As String)
        Dim parsedVanillaJson = JsonUtils.Parse(vanillaJson)
        Dim combinedJson = parsedVanillaJson
        Dim parsedForgeJson = JsonUtils.Parse(forgeJson)

        combinedJson.mainClass = parsedForgeJson.mainClass

        Dim forgeFiles As New List(Of MinecraftFile)

        For Each library In parsedForgeJson.libraries
            Dim file As New MinecraftFile() With {
                .path = library.downloads.artifact.path,
                .url = library.downloads.artifact.url,
                .sha1 = library.downloads.artifact.sha1
            }
            If file.url = "" Then
                Continue For
            Else
                forgeFiles.Add(file)
                combinedJson.libraries.Add(library)
            End If
        Next

        Dim extendedJvmParam As String = ""
        Dim extendedGameParam As String = ""

        For Each param In parsedForgeJson.arguments.game
            extendedGameParam += param + " "
        Next

        For Each param In parsedForgeJson.arguments.jvm
            extendedJvmParam += param + " "
        Next

        Dim returns As New With {
            combinedJson,
            extendedJvmParam,
            extendedGameParam,
            .files = forgeFiles
        }

        Return returns
    End Function
End Class