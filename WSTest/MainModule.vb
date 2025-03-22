Module MainModule

    Public Sub ErrorReport(ex As Exception)
        MessageBox.Show(ex.ToString, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub

End Module
