Imports System.Collections.Concurrent
Imports System.IO
Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Threading

Public Class MinecraftVersion
    Public name As String
    Public releaseTime As String
    Public type As String
    Public url As String

    Public Sub New(name As String, releaseTime As String, type As String, url As String)
        Me.name = name
        Me.releaseTime = releaseTime
        Me.type = type
        Me.url = url
    End Sub
End Class

Public Class NetworkException
    Inherits Exception
    Public Sub New()
        MyBase.New("A network exception occured, please check your network connection.")
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class

Public Class VersionNotFoundException
    Inherits Exception
    Public Sub New()
        MyBase.New("Version not found.")
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class

''' <summary>
''' 游戏下载器
''' </summary>
Public Class GameDownloader
    Private ReadOnly _minecraftVersions = New List(Of MinecraftVersion)
    Private ReadOnly _client As New HttpClient()
    Private ReadOnly _hostProvider As HostProvider
    Private Shared _httpUtils As HttpUtils

    Private Const _MAX_RETRIES = 20
    Private ReadOnly _successCount = 0
    Private ReadOnly _failureCount = 0

    Public mirror As String
    Private _totalCount As Integer
    Private ReadOnly _batchSize As Integer

    Private ReadOnly _semaphore

    ''' <summary>
    ''' 实例化一个游戏下载器
    ''' </summary>
    ''' <param name="mirror">下载源名称</param>
    ''' <param name="maxConcurrent">最大并发数，默认为64</param>
    ''' <param name="batchSize">下载器使用了分批下载的逻辑，批数默认为500，不建议改动</param>
    Public Sub New(mirror As String, Optional maxConcurrent As Integer = 64, Optional batchSize As Integer = 500)
        Me.mirror = mirror
        Me._batchSize = batchSize

        _hostProvider = New HostProvider(mirror)
        _httpUtils = New HttpUtils()
        _semaphore = New SemaphoreSlim(maxConcurrent)
    End Sub
    Private Async Function GetVersionManifest() As Task(Of String)
        Try
            Dim content = Await _httpUtils.GetAsync($"{_hostProvider.mirrors("pistonMeta")}/mc/game/version_manifest.json")
            Return content
        Catch ex As Exception
            Throw New NetworkException()
        End Try
    End Function

    Private Sub ParseVerionManifest()
        Dim raw = GetVersionManifest().Result
        Dim jsonParser = New JsonUtils(raw)
        For Each i In jsonParser.GetArray("versions")
            Dim id = JsonUtils.GetValueFromJson(i.ToString(), "id")
            Dim releaseTime = JsonUtils.GetValueFromJson(i.ToString(), "releaseTime")
            Dim url = JsonUtils.GetValueFromJson(i.ToString(), "url")
            Dim type = JsonUtils.GetValueFromJson(i.ToString(), "type")
            _minecraftVersions.Add(New MinecraftVersion(id, releaseTime, type, url))
        Next
    End Sub

    Private Function SearchTheVersion(version As String) As MinecraftVersion
        For Each i In _minecraftVersions
            If version = i.name Then
                Return i
            End If
        Next
        Throw New VersionNotFoundException($"Version {version} not found, please check if your input is correct.")
    End Function

    ''' <summary>
    ''' 下载游戏的索引文件
    ''' </summary>
    ''' <param name="root">定位到.minecraft文件夹的根目录</param>
    ''' <param name="requiredVersion">请求下载的版本</param>
    ''' <param name="customName">自定义名称，无则默认为版本号</param>
    Private Sub DownloadIndex(root As String, requiredVersion As String, Optional customName As String = "")
        If customName = "" Then
            customName = requiredVersion
        End If

        ParseVerionManifest()
        Dim SearchedVersion = SearchTheVersion(requiredVersion)
        Dim versionDir = $"{root}\versions\{customName}"
        If Not Directory.Exists(versionDir) Then
            Directory.CreateDirectory(versionDir)
        End If
        If Not Directory.Exists($"{root}\assets\indexes") Then
            Directory.CreateDirectory($"{root}\assets\indexes")
        End If
        Dim content = _httpUtils.GetAsync(_hostProvider.TransformToTargetMirror(SearchedVersion.url)).Result
        File.WriteAllText($"{versionDir}\{customName}.json", content)

        Dim versionManifest = File.ReadAllText($"{versionDir}\{customName}.json")
        Dim resourcesIndexUrl = JsonUtils.GetValueFromJson(versionManifest, "assetIndex.url")
        Dim indexId = JsonUtils.GetValueFromJson(versionManifest, "assetIndex.id")
        Dim result = _httpUtils.GetAsync(_hostProvider.TransformToTargetMirror(resourcesIndexUrl)).Result
        File.WriteAllText($"{root}\assets\indexes\{indexId}.json", result)
    End Sub
    ''' <summary>
    ''' 下载游戏的主方法
    ''' </summary>
    ''' <param name="root">.minecraft根目录</param>
    ''' <param name="requiredVersion">请求下载的版本</param>
    ''' <param name="customName">自定义名称，确保不与既有版本冲突</param>
    ''' <returns></returns>
    Public Async Function DownloadGameAsync(root As String, requiredVersion As String, Optional customName As String = "") As Task
        DownloadIndex(root, requiredVersion, customName)

        If customName = "" Then
            customName = requiredVersion
        End If

        Dim versionDir = $"{root}\versions\{customName}"

        Dim files = GameIndexParser.ParseIndex(
            customName,
            $"{root}\versions\{customName}\{customName}.json",
            root,
            mirror)

        Await DownloadFileAsync(files)
    End Function

    Private Async Function DownloadFileAsync(files As List(Of MinecraftFile)) As Task
        _totalCount = files.Count
        Dim stopwatch As Stopwatch = Stopwatch.StartNew()

        For i As Integer = 0 To _totalCount - 1 Step _batchSize
            Dim batchFiles = files.Skip(i).Take(_batchSize).ToList()
            Dim tasks As New ConcurrentBag(Of Task)()
            For Each file In batchFiles
                tasks.Add(ProcessFileWithSemaphoreAsync(file))
            Next
            Await Task.WhenAll(tasks)
            Console.WriteLine($"Completed batch {Math.Ceiling((i + _batchSize) / _batchSize)}: {batchFiles.Count} files processed.")
        Next

        stopwatch.Stop()
        Console.WriteLine($"Finished in {stopwatch.Elapsed.TotalSeconds}s. Total {_totalCount} file(s), {_successCount} passed, {_failureCount} missed.")
    End Function

    Private Async Function ProcessFileWithSemaphoreAsync(file As MinecraftFile) As Task
        Await _semaphore.WaitAsync()
        Try
            Await ProcessFileAsync(file)
        Finally
            _semaphore.Release()
        End Try
    End Function

    Private Async Function ProcessFileAsync(fileInfo As MinecraftFile) As Task
        If Not Directory.Exists(Path.GetDirectoryName(fileInfo.path)) Then
            Directory.CreateDirectory(Path.GetDirectoryName(fileInfo.path))
        End If

        If File.Exists(fileInfo.path) Then
            If VerifyFile(fileInfo.path, fileInfo.sha1) Then
                SyncLock Me
                    Interlocked.Increment(_successCount)
                    Console.WriteLine($"File already exists: {fileInfo.path}, {_totalCount - _successCount} remaining")
                End SyncLock
                Return
            Else
                File.Delete(fileInfo.path)
                Console.WriteLine($"Deleted {fileInfo.path}")
            End If
        End If

        Dim attempts As Integer = 0
        Dim success As Boolean = False

        While attempts < _MAX_RETRIES AndAlso Not success
            Try
                Dim response As HttpResponseMessage = Await _client.GetAsync(fileInfo.url)
                response.EnsureSuccessStatusCode()

                Using fileStream As New FileStream(fileInfo.path, FileMode.Create, FileAccess.Write, FileShare.None)
                    Await response.Content.CopyToAsync(fileStream)
                End Using

                If VerifyFile(fileInfo.path, fileInfo.sha1) Then
                    success = True
                    SyncLock Me
                        Interlocked.Increment(_successCount)
                    End SyncLock
                Else
                    Throw New Exception($"Failed to verify sha1 of {fileInfo.path}")
                End If
            Catch ex As Exception
                attempts += 1
                Console.WriteLine($"Failed: {fileInfo.url} to {fileInfo.path}, due to {ex} (retry: {attempts}/{_MAX_RETRIES})")
                If attempts >= _MAX_RETRIES Then
                    SyncLock Me
                        Interlocked.Increment(_failureCount)
                    End SyncLock
                End If
            End Try
        End While
        LogDownloadStatusAsync(fileInfo)
    End Function

    Public Sub LogDownloadStatusAsync(file As MinecraftFile)
        SyncLock Me
            Dim remainingFiles As Integer = _totalCount - (_successCount + _failureCount)
            Console.WriteLine($"Downloaded: {file.path}, {remainingFiles} remaining")
        End SyncLock
    End Sub

    Private Shared Function VerifyFile(filePath As String, expectedSha1 As String) As Boolean
        Using sha1 As SHA1 = SHA1.Create()
            Using stream As FileStream = File.OpenRead(filePath)
                Dim hash As Byte() = sha1.ComputeHash(stream)
                Dim hashString As String = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()
                Return hashString.Equals(expectedSha1, StringComparison.InvariantCultureIgnoreCase)
            End Using
        End Using
    End Function
End Class