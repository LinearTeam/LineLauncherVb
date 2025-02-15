Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

Public Class Keys
    Public Shared Function GetRandomGUID() As String
        Return Guid.NewGuid().ToString()
    End Function
End Class

Public Class TokenProtection
    Private ReadOnly roamingDir = $"{Environment.GetEnvironmentVariable("systemdrive")}\Users\{Environment.UserName}\AppData\Roaming\.lmcvb"

    Private Sub CreateLocalDir()
        If Not Directory.Exists(roamingDir) Then
            Directory.CreateDirectory($"{roamingDir}\UserSecrets")
            File.Create($"{roamingDir}\UserSecrets\secrets.line").Close()
        End If
    End Sub

    Private Shared Function CalculateSha1(psword As String) As String
        Dim encPassword As String = ""
        Dim sha As New SHA1CryptoServiceProvider()
        Dim bytesToHash() As Byte
        bytesToHash = System.Text.Encoding.ASCII.GetBytes(psword)
        bytesToHash = sha.ComputeHash(bytesToHash)
        For Each singleByte As Byte In bytesToHash
            encPassword += singleByte.ToString("x2")
        Next
        sha.Clear()
        Return encPassword
    End Function

    Private Function ReadPassword(username As String)
        Dim key = LineFileUtils.Read($"{roamingDir}\UserSecrets\secrets.line", username, username)
        Return key
    End Function

    Private Shared Function Encrypt(data As String, key As String) As String
        Using aesContainer = Aes.Create()
            Dim sha1 = Encoding.UTF8.GetBytes(key)
            Dim truncatedHash(31) As Byte
            Array.Copy(sha1, truncatedHash, 32)
            aesContainer.Key = truncatedHash
            Dim bytes(15) As Byte
            aesContainer.IV = bytes
            Dim encryptor = aesContainer.CreateEncryptor(aesContainer.Key, aesContainer.IV)
            Using memoryStream As New MemoryStream()
                Using cryptoStream As New CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write)
                    Using streamWriter As New StreamWriter(cryptoStream)
                        streamWriter.Write(data)
                    End Using
                End Using
                Return Convert.ToBase64String(memoryStream.ToArray())
            End Using
        End Using
    End Function

    Public Shared Function Decrypt(cipherText As String, key As String) As String
        Dim plain As String
        Using aesContainer As Aes = Aes.Create()
            Dim sha1 = Encoding.UTF8.GetBytes(key)
            Dim truncatedHash(31) As Byte
            Array.Copy(sha1, truncatedHash, 32)
            aesContainer.Key = truncatedHash
            aesContainer.IV = New Byte(15) {} ' 16字节初始化向量（全0）

            Dim decryptor As ICryptoTransform = aesContainer.CreateDecryptor(aesContainer.Key, aesContainer.IV)

            Using memoryStream As New MemoryStream(Convert.FromBase64String(cipherText))
                Using crytoStream As New CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read)
                    Using streamReader As New StreamReader(crytoStream)
                        plain = streamReader.ReadToEnd()
                    End Using
                End Using
            End Using
        End Using
        Return plain
    End Function

    ''' <summary>
    ''' 读取指定用户的数据
    ''' </summary>
    ''' <param name="username"></param>
    ''' <returns></returns>
    Public Function ReadUserInfo(username As String) As String
        Dim psw = ReadPassword(username)
        Dim infoCipher = LineFileUtils.Read(".\Data\Accounts.line", username, username)
        Dim info = Decrypt(infoCipher, psw)
        Return info
    End Function

    ''' <summary>
    ''' 写入加密的用户数据
    ''' </summary>
    ''' <param name="data">数据</param>
    ''' <param name="username">用户名</param>
    Public Sub WriteUserInfo(data As String, username As String)
        CreateLocalDir()
        Dim key = CalculateSha1(Keys.GetRandomGUID())
        LineFileUtils.Write($"{roamingDir}\UserSecrets\secrets.line", username, key, username)
        Dim dataCipher = Encrypt(data, key)
        LineFileUtils.Write(".\Data\Accounts.line", username, dataCipher, username)
    End Sub
End Class