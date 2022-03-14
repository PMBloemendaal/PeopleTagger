Imports System.Drawing
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Xml
Imports System.Windows.Shell
Public Class PeopleExtractorr
    Dim strExportDir As String
    Dim strScanDir As String
    Public ImageCollection As New Collection()
    Public FacesDetected As Integer = 0
    Dim PictureBoxOrigWidth As Integer
    Dim PictureBoxOrigHeight As Integer
    Private _backGND As New System.ComponentModel.BackgroundWorker
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me._backGND.WorkerReportsProgress = True
    End Sub
    Private Sub PeopleExtractor_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        strExportDir = GetSetting("PeopleExtractor", "Setting", "ExportDir", "")
        If strExportDir = "" Then
            strExportDir = My.Computer.FileSystem.SpecialDirectories.MyPictures & "\faces"
        End If
        SaveSetting("PeopleExtractor", "Setting", "ExportDir", strExportDir)
        txtExport.Text = strExportDir

        strScanDir = GetSetting("PeopleExtractor", "Setting", "ScanDir", "")
        If strScanDir = "" Then
            strScanDir = My.Computer.FileSystem.SpecialDirectories.MyPictures
        End If
        SaveSetting("PeopleExtractor", "Setting", "ScanDir", strScanDir)
        txtScan.Text = strScanDir

        PictureBoxOrigWidth = PictureBox1.Width
        PictureBoxOrigHeight = PictureBox1.Height


    End Sub

    Private Sub butGO_Click(sender As Object, e As EventArgs) Handles butGO.Click
        FacesDetected = 0
        PictureBox2.Visible = False

        strScanDir = txtScan.Text
        strExportDir = txtExport.Text
        If Not Directory.Exists(strExportDir) Then
            Directory.CreateDirectory(strExportDir)
        End If

        Dim myTaskbarInfo As New TaskbarItemInfo
        myTaskbarInfo.ProgressState = TaskbarItemProgressState.Normal

        'Call DetectFaces("C:\Users\PMBloemendaal\Pictures\Fotos\2009\Kerst 2009\Willeke Debbie en Annet klaar voor kerst.jpg")
        'Call DetectFaces("C:\Users\PMBloemendaal\Pictures\Fotos\2010\20100403 Eten Schoonderwaldt\2259 Iris Jeroen en Pieter op de bank.jpg")
        'Exit Sub

        Call FillCollection(strScanDir, "*.jpg")

        ToolStripProgressBar1.Maximum = ImageCollection.Count
        Dim n As Integer
        For n = 1 To ImageCollection.Count
            ToolStripProgressBar1.Value = n
            Call DetectFaces(ImageCollection.Item(n).ToString)
            myTaskbarInfo.ProgressValue = CDbl(n / ImageCollection.Count)
            Application.DoEvents()
        Next
        PictureBox2.Visible = True
        PictureBox1.Image = PictureBox2.Image
    End Sub
    Public Sub FillCollection(ByVal thePath As String, ByVal theWildCard As String)
        Dim i As Integer
        Dim Files() As String = Directory.GetFiles(thePath, theWildCard)
        Dim Dirs() As String = Directory.GetDirectories(thePath)

        For i = 0 To UBound(Files)
            ImageCollection.Add(Files(i))
        Next

        For i = 0 To UBound(Dirs)
            FillCollection(Dirs(i), theWildCard)
        Next
    End Sub
    Private Sub DetectFaces(strFileName As String)

        PictureBox1.Image = Bitmap.FromFile(strFileName)
        ToolStripStatusLabel1.Text = "Processing file : " & strFileName

        PictureBox1.Height = PictureBoxOrigHeight
        PictureBox1.Width = PictureBoxOrigWidth
        Dim ratioPictureBox As Single = CSng(PictureBox1.Width / PictureBox1.Height)
        Dim ratioImage As Single = CSng(PictureBox1.Image.Width / PictureBox1.Image.Height)

        If ratioImage > ratioPictureBox Then ' Landscape
            PictureBox1.Height = CInt(PictureBox1.Height / ratioImage)
        End If
        If ratioPictureBox > ratioImage Then    ' Portrait
            PictureBox1.Width = CInt(PictureBox1.Width * ratioImage)
        End If

        Application.DoEvents()

        Debug.Print(Path.GetFileName(strFileName))
        Dim infoReader As System.IO.FileInfo
        infoReader = My.Computer.FileSystem.GetFileInfo(strFileName)
        Dim fileSize As Long = infoReader.Length


        Dim xDoc As New XmlDocument
        xDoc = getXMP(strFileName)

        'Debug.Print("<hr/>Zoek elementen Description met attribuut Name zonder gebruik te maken van namespaces<br/>")
        Dim MetadataDate As Date
        If Not IsNothing(xDoc.SelectSingleNode("//*[local-name()='Description']/@*[local-name()='MetadataDate']")) Then MetadataDate = CDate(xDoc.SelectSingleNode("//*[local-name()='Description']/@*[local-name()='MetadataDate']").Value)
        Dim CreateDate As Date
        If Not IsNothing(xDoc.SelectSingleNode("//*[local-name()='Description']/@*[local-name()='CreateDate']")) Then CreateDate = CDate(xDoc.SelectSingleNode("//*[local-name()='Description']/@*[local-name()='CreateDate']").Value)
        Dim ModifyDate As Date
        If Not IsNothing(xDoc.SelectSingleNode("//*[local-name()='Description']/@*[local-name()='ModifyDate']")) Then ModifyDate = CDate(xDoc.SelectSingleNode("//*[local-name()='Description']/@*[local-name()='ModifyDate']").Value)
        Dim DateTimeOriginal As Date
        If Not IsNothing(xDoc.SelectSingleNode("//*[local-name()='Description']/@*[local-name()='DateTimeOriginal']")) Then DateTimeOriginal = CDate(xDoc.SelectSingleNode("//*[local-name()='Description']/@*[local-name()='DateTimeOriginal']").Value)
        Dim unit As String
        If Not IsNothing(xDoc.SelectSingleNode("//*[local-name()='AppliedToDimensions']/@*[local-name()='unit']")) Then unit = xDoc.SelectSingleNode("//*[local-name()='AppliedToDimensions']/@*[local-name()='unit']").Value
        Dim DimH As Integer
        If Not IsNothing(xDoc.SelectSingleNode("//*[local-name()='AppliedToDimensions']/@*[local-name()='h']")) Then DimH = CInt(xDoc.SelectSingleNode("//*[local-name()='AppliedToDimensions']/@*[local-name()='h']").Value)
        Dim DimW As Integer
        If Not IsNothing(xDoc.SelectSingleNode("//*[local-name()='AppliedToDimensions']/@*[local-name()='w']")) Then DimW = CInt(xDoc.SelectSingleNode("//*[local-name()='AppliedToDimensions']/@*[local-name()='w']").Value)

        '        For Each el As System.Xml.XmlNode In xDoc.SelectNodes("//*[local-name()='Description' and @*[local-name()='Name']]")
        For Each el As System.Xml.XmlNode In xDoc.SelectNodes("//*[local-name()='Description' and @*[local-name()='Name'] and @*[local-name()='Type']]")
            'use namespaceURI for attribute!
            Try


                If el.Attributes("Type", "http://www.metadataworkinggroup.com/schemas/regions/").Value = "Face" Then
                    ' Face Detected

                    FacesDetected += 1
                    Debug.Print("<p><i>" & el.Attributes("Name", "http://www.metadataworkinggroup.com/schemas/regions/").Name & " : " & el.Attributes("Name", "http://www.metadataworkinggroup.com/schemas/regions/").Value & "</i></p>")
                    Dim strFaceName As String
                    strFaceName = el.Attributes("Name", "http://www.metadataworkinggroup.com/schemas/regions/").Value
                    strFaceName = Regex.Replace(strFaceName, "[^[a-zA-Z0-9_@.-]]*", "_")

                    Dim X, Y, W, H As Single
                    For Each i As System.Xml.XmlAttribute In el.Attributes
                        Debug.Print("<p>" & i.InnerText & " : " & i.Name & " : " & i.Value & "</p>")

                        Dim a As System.Xml.XmlNode = el.SelectSingleNode("*[local-name()='Area']")
                        If Not a Is Nothing Then
                            For Each j As System.Xml.XmlAttribute In a.Attributes
                                Debug.Print("<p>" & j.Name & " : " & j.Value & "</p>")
                                Select Case j.Name
                                    Case "stArea:x"
                                        X = CSng(j.Value)
                                    Case "stArea:y"
                                        Y = CSng(j.Value)
                                    Case "stArea:w"
                                        W = CSng(j.Value)
                                    Case "stArea:h"
                                        H = CSng(j.Value)
                                End Select
                            Next
                        End If

                    Next
                    If CreateDate < #1900-01-01# Then
                        CreateDate = MetadataDate
                    End If
                    If DateTimeOriginal < #1900-01-01# Then
                        DateTimeOriginal = CreateDate
                    End If

                    ProcessFace(strFileName, DimW, DimH, DateTimeOriginal, CreateDate, ModifyDate, MetadataDate, strFaceName, X, Y, W, H)
                    ToolStripStatusLabel2.Text = "Faces detected: " & FacesDetected
                    Application.DoEvents()
                End If
            Catch ex As Exception
                Beep()
            End Try
        Next

    End Sub
    Private Sub ProcessFace(strFileName As String, DimW As Integer, DimH As Integer,
                            DateTimeOriginal As Date, CreateDate As Date, ModifyDate As Date, MetadataDate As Date,
                            strFaceName As String, X As Single, Y As Single, W As Single, H As Single)

        Dim newSection As New Bitmap(CInt(W * DimW), CInt(H * DimH))
        newSection = getSection(CInt(X * DimW) - CInt(0.5 * W * DimW), CInt(Y * DimH) - CInt(0.5 * H * DimH), CInt(W * DimW), CInt(H * DimH))

        ' Create a directory (if needed) for this facename
        If Not Directory.Exists(strExportDir & "\" & strFaceName) Then
            Directory.CreateDirectory(strExportDir & "\" & strFaceName)
        End If
        Dim strSaveFile As String
        Dim n As Integer = 1
        strSaveFile = CreateDate.ToString("yyyymmdd-HHmmss")
        While File.Exists(strExportDir & "\" & strFaceName & "\" & strSaveFile & ".jpg")
            strSaveFile = CreateDate.ToString("yyyymmdd-HHmmss") & " " & n
            n += 1
        End While
        newSection.Save(strExportDir & "\" & strFaceName & "\" & strSaveFile & ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg)


    End Sub
    Private Function getSection(x As Integer, y As Integer, w As Integer, h As Integer) As Bitmap

        ' Make a Bitmap to hold the result.
        Dim bm As New Bitmap(w, h)

        ' Associate a Graphics object with the Bitmap
        Using gr As Graphics = Graphics.FromImage(bm)
            ' Define source and destination rectangles.
            Dim src_rect As New Rectangle(x, y, w,
                h)
            Dim dst_rect As New Rectangle(0, 0, w, h)

            ' Copy that part of the image.
            gr.DrawImage(PictureBox1.Image, dst_rect, src_rect,
                GraphicsUnit.Pixel)

            Application.DoEvents()

        End Using


        Return bm
    End Function
    Public Function getXMP(strFileName As String) As XmlDocument
        Dim b() As Byte = IO.File.ReadAllBytes(strFileName)

        Dim po1, po2 As Integer
        po1 = inBytes(b, "<x:xmpmeta")
        po2 = inBytes(b, "</x:xmpmeta>")

        Dim b2() As Byte = New Byte(po2 - po1 + 11) {}
        Array.Copy(b, po1, b2, 0, po2 - po1 + 12)
        Dim strXMP As String = System.Text.Encoding.ASCII.GetString(b2)

        Dim doc As New XmlDocument
        Try
            doc.LoadXml(strXMP)
        Catch ex As Exception
        End Try

        Return doc
    End Function

    '    End Function
    Private Function inBytes(b As Byte(), strSearch As String) As Integer
        Dim encoding As New System.Text.ASCIIEncoding
        Dim bSearch As Byte() = encoding.GetBytes(strSearch)
        Dim bFound As Boolean = True

        For i As Integer = 0 To b.Length - bSearch.Length - 1
            If b(i) = bSearch(0) Then
                bFound = True
                For j As Integer = 0 To bSearch.Length - 1
                    If b(i + j) <> bSearch(j) Then
                        bFound = False
                        Exit For
                    End If
                Next
                If bFound Then
                    Return i
                End If
            End If
        Next
        Return Nothing
    End Function
    Function getNameSpaceMgr(ByVal xDoc As System.Xml.XmlDocument) As System.Xml.XmlNamespaceManager
        Dim nsmgr As New System.Xml.XmlNamespaceManager(xDoc.NameTable)
        'Add namespaces with prefixes used in XPath
        nsmgr.AddNamespace("RDF", "http://www.w3.org/1999/02/22-rdf-syntax-ns#")
        nsmgr.AddNamespace("MWG-rs", "http://www.metadataworkinggroup.com/schemas/regions/")
        'Add other namespaces using the prefixes from xDoc. Better use your own prefix (like above)
        For Each i As System.Xml.XmlNode In xDoc.SelectNodes("//namespace::*")
            If i.Name.StartsWith("xmlns:") Then
                If nsmgr.LookupPrefix(i.Value) Is Nothing Then
                    nsmgr.AddNamespace(i.Name.Replace("xmlns:", ""), i.Value)
                End If
            End If
        Next
        Return nsmgr
    End Function

    Private Sub butSelectExport_Click(sender As Object, e As EventArgs) Handles butSelectExport.Click
        If System.IO.Directory.Exists(txtExport.Text) Then
            FolderBrowserDialog1.SelectedPath = txtExport.Text
        End If
        FolderBrowserDialog1.ShowNewFolderButton = True
        FolderBrowserDialog1.ShowDialog()

        txtExport.Text = FolderBrowserDialog1.SelectedPath
        strExportDir = txtExport.Text
        SaveSetting("PeopleExtractor", "Setting", "ExportDir", strExportDir)

    End Sub

    Private Sub txtExport_TextChanged(sender As Object, e As EventArgs) Handles txtExport.TextChanged
        strExportDir = txtExport.Text
        SaveSetting("PeopleExtractor", "Setting", "ExportDir", strExportDir)

    End Sub

    Private Sub butSelectScan_Click(sender As Object, e As EventArgs) Handles butSelectScan.Click
        If System.IO.Directory.Exists(txtScan.Text) Then
            FolderBrowserDialog1.SelectedPath = txtScan.Text
        End If
        FolderBrowserDialog1.ShowNewFolderButton = False
        FolderBrowserDialog1.ShowDialog()

        txtScan.Text = FolderBrowserDialog1.SelectedPath
        strScanDir = txtScan.Text
        SaveSetting("PeopleExtractor", "Setting", "ScanDir", strScanDir)
    End Sub

    Private Sub txtScan_TextChanged(sender As Object, e As EventArgs) Handles txtScan.TextChanged
        strScanDir = txtScan.Text
        SaveSetting("PeopleExtractor", "Setting", "ScanDir", strScanDir)

    End Sub
End Class
