Class Application
    Sub Main(ByVal cmdArgs() As String)

    End Sub
    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        Dim mainWindowName As String = Guid.NewGuid().ToString()

        Dim top As String = vbNull, left As String = vbNull, height As String = vbNull, width As String = vbNull, componentType As String = vbNull, uuid As String = vbNull
        If (e.Args.Length > 0) Then
            For i = 0 To e.Args.Length
                Dim argument = e.Args(i).Split(New Char() {"="}, 2)
                Dim argumentName = argument(0)
                Dim argumentValue = argument(1)
                Select Case argumentName
                    Case "top"
                        top = argumentValue
                    Case "left"
                        left = argumentValue
                    Case "width"
                        width = argumentValue
                    Case "height"
                        height = argumentValue
                    Case "finsembleWindowName"
                        mainWindowName = argumentValue
                    Case "componentType"
                        componentType = argumentValue
                    Case "uuid"
                        uuid = argumentValue
                End Select
            Next
        End If

        Dim MainWindow = New MainWindow(mainWindowName, componentType, top, left, height, width, uuid)
    End Sub

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

End Class
