Imports System.IO
Imports System.Net
Imports System.Text

Public Class Form1

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Try

            Dim url As String = "http://api.homeweatherhub.com/Current/"
            'Dim url As String = "https://localhost:44367/Current/"

            Dim formData As String = "PASSKEY=64342F32A039AFA8CACC2061B1A77938"

            Dim request As WebRequest = WebRequest.Create(url)
            Dim response As WebResponse
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(formData)

            request.ContentType = "application/x-www-form-urlencoded"
            request.Method = "POST"
            'request.ContentType = "application/form-data"
            'request.ContentType = "application/json"
            request.ContentLength = bytes.Length
            Dim stream As Stream = request.GetRequestStream()

            stream.Write(bytes, 0, bytes.Length)
            stream.Close()

            response = request.GetResponse()

            Using sr As New StreamReader(response.GetResponseStream())
                MsgBox(sr.ReadToEnd)
            End Using

        Catch ex As Exception
            ErrorReport(ex)
        End Try

    End Sub

    'Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

    '    Try

    '        'Dim url As String = "http://api.homeweatherhub.com/ws/"
    '        Dim url As String = "https://localhost:44367/ws/"

    '        Dim obj As New WSData With {.PASSKEY = "1234567890"}
    '        Dim jsonData As String = Newtonsoft.Json.JsonConvert.SerializeObject(obj)

    '        Dim request As WebRequest = WebRequest.Create(url)
    '        Dim response As WebResponse
    '        Dim bytes As Byte() = Encoding.UTF8.GetBytes(jsonData)

    '        request.Method = "POST"
    '        'request.ContentType = "application/x-www-form-urlencoded"
    '        request.ContentType = "application/json"
    '        request.ContentLength = bytes.Length

    '        Dim stream As Stream = request.GetRequestStream()
    '        stream.Write(bytes, 0, bytes.Length)
    '        stream.Close()

    '        response = request.GetResponse()

    '        Using sr As New StreamReader(response.GetResponseStream())
    '            MsgBox(sr.ReadToEnd)
    '        End Using

    '    Catch ex As Exception
    '        ErrorReport(ex)
    '    End Try

    'End Sub

    Public Class WSData

        Public Property PASSKEY As String
        Public Property stationtype As String
        Public Property dateutc As String
        Public Property tempinf As String
        Public Property humidityin As String
        Public Property baromrelin As String
        Public Property baromabsin As String
        Public Property tempf As String
        Public Property humidity As String
        Public Property winddir As String
        Public Property windspeedmph As String
        Public Property windgustmph As String
        Public Property maxdailygust As String
        Public Property rainratein As String
        Public Property eventrainin As String
        Public Property hourlyrainin As String
        Public Property dailyrainin As String
        Public Property weeklyrainin As String
        Public Property monthlyrainin As String
        Public Property totalrainin As String
        Public Property solarradiation As String
        Public Property uv As String
        Public Property wh65batt As String
        Public Property freq As String
        Public Property model As String

    End Class

End Class
