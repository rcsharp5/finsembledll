Imports ChartIQ.Finsemble
Imports Newtonsoft.Json.Linq
Imports System.Threading.Tasks

Class MainWindow
    Private finsemble As FinsembleBridge
    Private windowName, top, left, height, width, uuid As String
    Private componentType As String = "Unknown"

    Public Sub New(FinsembleWindowName As String, componentType As String, top As String, left As String, height As String, width As String, uuid As String)

        If Not String.IsNullOrEmpty(FinsembleWindowName) Then
            windowName = FinsembleWindowName
        Else
            windowName = Guid.NewGuid().ToString()
        End If

        If Not String.IsNullOrEmpty(componentType) Then
            Me.componentType = componentType
        End If

        Me.top = top
        Me.left = left
        Me.height = height
        Me.width = width
        Me.uuid = uuid

        finsemble = New FinsembleBridge(New System.Version("8.56.28.34"), windowName, componentType, Me, uuid)
        finsemble.Connect()
        AddHandler finsemble.Connected, AddressOf Bridge_Connected

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Private Sub InitializeEverything()
        InitializeComponent()
        FinsembleHeader.setBridge(finsemble)
        If (Not String.IsNullOrEmpty(top)) Then
            Me.top = Double.Parse(top)
        End If

        If (Not String.IsNullOrEmpty(left)) Then
            Me.left = Double.Parse(left)
        End If

        If (Not String.IsNullOrEmpty(height)) Then
            Me.height = Double.Parse(height)
        End If

        If (Not String.IsNullOrEmpty(width)) Then
            Me.width = Double.Parse(width)
        End If
        Me.Show()
    End Sub

    Private Sub Bridge_Connected(sender As Object, e As EventArgs)
        Application.Current.Dispatcher.Invoke(AddressOf InitializeEverything)
        Dim SubscribeParams As JObject = New JObject()
        SubscribeParams("dataType") = "symbol"
        finsemble.SendCommand("LinkerClient.subscribe", SubscribeParams, AddressOf SubscribeHandler)
    End Sub

    Private Sub BlankHandler(e As String, args As FinsembleEventArgs)

    End Sub

    Private Sub MainThreadSubscribe(ByVal args As FinsembleEventArgs)
        DataToSend.Text = args.response("data").ToString()
    End Sub

    Private Sub SubscribeHandler(e As String, args As FinsembleEventArgs)
        Application.Current.Dispatcher.Invoke(Sub() MainThreadSubscribe(args))
    End Sub

    Private Sub Send_Click(sender As Object, e As RoutedEventArgs)

        finsemble.SendCommand("LinkerClient.publish", New JObject(), AddressOf BlankHandler)
    End Sub
End Class
