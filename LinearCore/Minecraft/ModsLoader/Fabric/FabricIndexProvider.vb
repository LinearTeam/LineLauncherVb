Imports System.Dynamic

Class FabricIndexProvider
    Private ReadOnly _httpUtils As New HttpUtils()
    Private ReadOnly _hostProvider As HostProvider

    Public Sub New(mirror As String)
        _hostProvider = New HostProvider(mirror)
    End Sub

    ''' <summary>
    ''' Get the list of available fabric versions from the fabric mirror.
    ''' </summary>
    Public Async Function GetFabricManifest() As Task(Of List(Of String))
        Dim rawManifest = Await _httpUtils.GetAsync(_hostProvider.provideMirror.fabric)
        Dim parsedManifest = JsonUtils.Parse(rawManifest)
        Dim loaders = parsedManifest.loader
        Dim availVersions As New List(Of String)
        For Each i In loaders
            availVersions.Add(i.version)
        Next
        Return availVersions
    End Function

    ''' <summary>
    ''' Get the fabric version for a specific Minecraft version.
    ''' </summary>
    Public Async Function QueryFabricJson(minecraftVersion As String, fabricVersion As String) As Task(Of String)
        Return Await _httpUtils.GetAsync($"{_hostProvider.provideMirror.fabric}/loader/{minecraftVersion}/{fabricVersion}/profile/json")
    End Function

    ''' <summary>
    ''' Combine the vanilla JSON with the fabric JSON.
    ''' This function will add the fabric libraries to the vanilla JSON and return the combined JSON.
    ''' It also returns a list of MinecraftFile objects for downloading.
    ''' </summary>
    ''' <param name="vanillaJson"></param>
    ''' <param name="fabricJson"></param>
    ''' <returns></returns>
    Public Shared Function Combine(vanillaJson As String, fabricJson As String) As Object
        Dim parsedVanillaJson = JsonUtils.Parse(vanillaJson)
        Dim parsedFabricJson = JsonUtils.Parse(fabricJson)
        Dim combinedFabricJson = parsedVanillaJson

        combinedFabricJson.mainClass = parsedFabricJson.mainClass

        Dim fabricFiles As New List(Of MinecraftFile)

        For Each library In parsedFabricJson.libraries
            Dim nameSeq = library.name.Split(":")
            Dim packageName = nameSeq(0)
            Dim libraryName = nameSeq(1)
            Dim libraryVersion = nameSeq(2)

            Dim path = $"{packageName.Replace(".", "/")}/{libraryName}/{libraryVersion}/{libraryName}-{libraryVersion}.jar"

            Dim libraryDict = DirectCast(library, IDictionary(Of String, Object))
            libraryDict.Remove("md5")
            libraryDict.Remove("sha256")
            libraryDict.Remove("sha512")

            library.path = path
            library.url = $"{library.url}{path}"

            Dim fabricFile As New MinecraftFile() With {
                .url = library.url,
                .path = library.path
            }

            Try
                fabricFile.sha1 = library.sha1
            Catch ex As Exception
                fabricFile.sha1 = Nothing
            End Try

            fabricFiles.Add(fabricFile)

            parsedVanillaJson.libraries.Add(library)
        Next

        Dim returns As Object = New ExpandoObject()

        returns.combinedJson = combinedFabricJson
        returns.files = fabricFiles

        Return returns
    End Function
End Class