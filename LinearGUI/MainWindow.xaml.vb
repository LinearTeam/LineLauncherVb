Imports iNKORE.UI.WPF.Helpers
Imports iNKORE.UI.WPF.Modern.Media.Animation

Class MainWindow
    Private HomePage As New Home()
    Private AccountsPage As New Accounts()
    Private GamesPage As New Games()
    Private GameManage As New GameManagement()
    Private DownloadsPage As New DownloadTasks()
    Private SettingsPage As New Settings()
    Private InfoPage As New Info()

    Public Sub Navigate(page As Page)
        If page IsNot Nothing Then
            MainFrame.Navigate(page, infoOverride:=New DrillInNavigationTransitionInfo)
            MainNavigation.Header = page.Title
        End If
    End Sub
    Private Sub MainNavigation_SelectionChanged(sender As iNKORE.UI.WPF.Modern.Controls.NavigationView, args As iNKORE.UI.WPF.Modern.Controls.NavigationViewSelectionChangedEventArgs)
        Dim target As Page = Nothing
        Dim page = args.SelectedItem

        If page.Equals(HomeIndicator) Then
            target = HomePage
        ElseIf page.Equals(AccountsIndicator) Then
            target = AccountsPage
        ElseIf page.Equals(GameManagementIndicator) Then
            target = GamesPage
        ElseIf page.Equals(SettingsIndicator) Then
            target = SettingsPage
        ElseIf page.Equals(DownloadsIndicator) Then
            target = DownloadsPage
        ElseIf page.Equals(AboutIndicator) Then
            target = InfoPage
        End If
        Navigate(target)
    End Sub

    Private Sub Window_SizeChanged(sender As Object, e As SizeChangedEventArgs)
        AccountsPage.UsersView.MaxHeight = Me.Height * 0.6
    End Sub
End Class