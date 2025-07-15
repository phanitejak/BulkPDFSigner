using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.X509;
using iText.Kernel.Pdf;
using iText.Signatures;
using Newtonsoft.Json.Linq;

namespace BulkPdfSigner;

public partial class Form1 : Form
{
    X509Certificate2? x509Certificate;
    string[] PathFileName = { };
    string targetloc = "";
    private string page_opt = "";
    string[] user_info = { "", "", "", "" };
    private bool _trial = false;
    private bool _activated = false;
    X509Certificate2Collection? x509Certificate2Collection;
    CultureInfo provider = CultureInfo.InvariantCulture;

    public Form1()
    {
        InitializeComponent();

        Text = $"Bulk PDF Signer - Version {Application.ProductVersion.Split('+')[0]}";
        Shown += Form1_Shown;
        FormClosed += Form1_FormClosed;
        FormClosing += Form1_FormClosing;

        msglabel.Visible = false;
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        MessageBoxManager.Unregister();
    }

    private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
    {
        MessageBoxManager.Unregister();
    }

    private void Form1_Shown(object? sender, EventArgs e)
    {
        status_msgbox.AppendText("Activating Software..." + Environment.NewLine);

        X509Store x509Store = new X509Store(StoreLocation.CurrentUser);
        x509Store.Open(OpenFlags.ReadOnly);
        x509Certificate2Collection = X509Certificate2UI.SelectFromCollection(x509Store.Certificates, "Select certificate", "", X509SelectionFlag.SingleSelection);
        x509Store.Close();

        if (x509Certificate2Collection.Count <= 0)
        {
            MessageBox.Show("No Certificate selected.");
            status_msgbox.AppendText(Environment.NewLine + "No Certificate Selected." + Environment.NewLine);
        }
        else
        {
            x509Certificate = x509Certificate2Collection[0];
            if (!get_userinfo(x509Certificate.GetNameInfo(X509NameType.SimpleName, false)))
            {
                if (_trial)
                {
                    MessageBoxManager.OK = "Start Trial";
                    MessageBoxManager.Cancel = "Quit";
                    DialogResult dres = MessageBox.Show("This product is not Registered.", "License Status", MessageBoxButtons.OKCancel);
                    MessageBoxManager.OK = "OK";
                    MessageBoxManager.Cancel = "Cancel";
                    if (dres == DialogResult.Cancel)
                    {
                        Application.Exit();
                    }
                }
                else
                {
                    Application.Exit();
                }
            }
        }
    }

    private bool get_userinfo(string user)
    {
        bool status = false;
        status_msgbox.AppendText("Contacting Licensing Server..." + Environment.NewLine);

        try
        {
            using (HttpClient client = new HttpClient())
            {
                string apiUrl = $"https://bulk-pdf-signer-license-provider.onrender.com/license?username={user}";
                client.DefaultRequestHeaders.Add("X-API-KEY", "***REDACTED-API-KEY***");

                HttpResponseMessage response = client.GetAsync(apiUrl).GetAwaiter().GetResult();
                string result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    var json = JObject.Parse(result);
                    user_info[0] = json["username"]?.ToString() ?? "";
                    user_info[1] = json["circle"]?.ToString() ?? "";
                    user_info[2] = json["valid_till"]?.ToString() ?? "";
                    string lic_type = json["lic_type"]?.ToString()?.Trim().ToUpperInvariant() ?? "";
                    user_info[3] = lic_type;
                    if (lic_type == "TRIAL")
                    {
                        _trial = true;
                    }

                    string validTillRaw = user_info[2];
                    DateTime validTillDate;

                    bool isDateValid = DateTime.TryParseExact(validTillRaw, "dd-MM-yyyy", provider, DateTimeStyles.None, out validTillDate);

                    if (!isDateValid)
                    {
                        status_msgbox.AppendText($"Invalid license date format: {validTillRaw}. Please contact administrator.\n");
                        MessageBox.Show($"License date format is invalid: {validTillRaw}. Please contact administrator.");
                        return false;
                    }

                    if (DateTime.Now > validTillDate)
                    {
                        status_msgbox.AppendText($"This software is licensed till {user_info[2]} only. Please contact administrator." + Environment.NewLine);
                        MessageBox.Show($"This software is licensed till {user_info[2]} only. Please contact administrator.");
                    }
                    else
                    {
                        MessageBox.Show($"License Found! Software Activated.\n\nLicensed to Mr. {user_info[0]} ({user_info[1]} Circle)\nValid till: {user_info[2]}", "Licensing Details");
                        status_msgbox.AppendText($"License Found! Software Activated. Licensed to Mr. {user_info[0]}, valid till {user_info[2]}" + Environment.NewLine);
                        status = true;
                        _activated = true;
                    }
                }
                else
                {
                    // Try creating trial license
                    status_msgbox.AppendText($"No license found for {user}. Trying trial license..." + Environment.NewLine);
                    var trialData = new
                    {
                        username = user,
                        circle = "Trial",
                        valid_till = DateTime.Now.AddDays(2).ToString("dd-MM-yyyy"),
                        lic_type = "Trial"
                    };

                    var jsonData = new StringContent(
                        Newtonsoft.Json.JsonConvert.SerializeObject(trialData),
                        System.Text.Encoding.UTF8,
                        "application/json");

                    HttpResponseMessage postResponse = client.PostAsync("https://bulk-pdf-signer-license-provider.onrender.com/license", jsonData).GetAwaiter().GetResult();

                    if (postResponse.IsSuccessStatusCode)
                    {
                        status_msgbox.AppendText("Trial license created successfully." + Environment.NewLine);
                        MessageBox.Show("Trial license created. You can now use the software for 2 days.");
                        _trial = true;
                        _activated = true;
                        user_info[0] = user;
                        user_info[1] = "Trial";
                        user_info[2] = trialData.valid_till;
                        user_info[3] = "ALL";
                    }
                    else
                    {
                        string errorDetail = postResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        MessageBox.Show($"Unable to create trial license.\n\n{errorDetail}");
                        status_msgbox.AppendText("Trial license creation failed." + Environment.NewLine);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            status_msgbox.AppendText("Error contacting license server: " + ex.Message + Environment.NewLine);
            MessageBox.Show("Error contacting license server: " + ex.Message);
        }

        return status;
    }


    public bool signPdfFile(string sourceDocument, string destinationPath, X509Certificate2 cert)
    {
        bool result = false;
        X509CertificateParser certParser = new X509CertificateParser();
        Org.BouncyCastle.X509.X509Certificate[] chain = [certParser.ReadCertificate(cert.RawData)];

        IExternalSignature externalSignature = new X509Certificate2Signature(cert, "SHA256");

        PdfDocument pdfDoc = new PdfDocument(new PdfReader(sourceDocument));
        int lastpage = pdfDoc.GetNumberOfPages();
        pdfDoc.Close();
        PdfReader pdfReader = new PdfReader(sourceDocument);
        FileStream dest_pdf = new FileStream(destinationPath, FileMode.Create, FileAccess.ReadWrite);
        PdfSigner pdfSigner = new PdfSigner(pdfReader, dest_pdf, new StampingProperties());

        try
        {
            PdfSignatureAppearance signatureAppearance = pdfSigner.GetSignatureAppearance();
            if (page_opt == "lastpage" && (user_info[3] == "ALL" || user_info[3] == "SACFA"))
            {
                // Create the signature appearance
                iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(36, 648, 200, 100);
                signatureAppearance
                    // Specify if the appearance before field is signed will be used
                    // as a background for the signed field. The "false" value is the default value.
                    .SetReuseAppearance(false)
                    .SetPageRect(rect)
                    .SetPageNumber(lastpage);
                pdfSigner.SetFieldName("signature1");
            }
            else
            {
                pdfSigner.SetFieldName("Signature 1");
            }
            signatureAppearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.NAME_AND_DESCRIPTION);
            pdfSigner.SignDetached(externalSignature, chain, null, null, null, 0, PdfSigner.CryptoStandard.CMS);
            result = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            pdfReader.Close();
        }
        return result;
    }

    private void aboutToolStrip_Click(object sender, EventArgs e)
    {
        if (_activated)
        {
            if (_trial)
            {
                MessageBox.Show("This software is designed by Phaniteja K. Please contact or Whatsapp on +91 9848003093, or Mail me at kondapalliphaniteja@gmail.com" + Environment.NewLine + Environment.NewLine + "This product is a Trial Version and Can be used for 2 days from the date of installation for 5 times. A maximum of 20 PDFs can be signed per usage.", "Licensing Details");
            }
            else
            {
                MessageBox.Show("This software is designed by Phaniteja K. Please contact or Whatsapp on +91 9848003093, or Mail me at kondapalliphaniteja@gmail.com" + Environment.NewLine + Environment.NewLine + "This product is licensed to Mr. " + user_info[0] + " of " + user_info[1] + " Circle & is valid till " + user_info[2], "Licensing Details");
            }
        }
        else
        {
            MessageBox.Show("This software is not activated either by trial or full. Please close and re-open software and select Licensed certificate to Activate.");
        }
    }

    private void beginsigning_Click_1(object sender, EventArgs e)
    {
        if (_activated && (DateTime.Now <= DateTime.ParseExact(user_info[2], "dd-MM-yyyy", provider)))
        {
            x509Certificate = (X509Certificate2)((X509CertificateCollection)x509Certificate2Collection)[0];
            if (PathFileName == null)
            {
                MessageBox.Show("No files selected.");
                status_msgbox.AppendText(Environment.NewLine);
                status_msgbox.AppendText("No Files Selected.");
                status_msgbox.AppendText(Environment.NewLine);
            }
            else if (targetloc == string.Empty)
            {
                targetloc = textBox1.Text + "\\Result\\";
                if (textBox2.Text == string.Empty)
                {
                    textBox2.Text = targetloc;
                }
                else if (!Directory.Exists(textBox2.Text))
                {
                    MessageBox.Show("Entered target location is not valid... Please enter and try again.");
                    status_msgbox.AppendText(Environment.NewLine);
                    status_msgbox.AppendText("Entered target location is not valid... Please enter and try again.");
                    status_msgbox.AppendText(Environment.NewLine);
                }
            }
            else if (PathFileName.Count() > 5 && _trial)
            {
                MessageBox.Show("This is trial version. You cannot select more than 5 files in one go.");
            }
            else
            {
                pgbar.Step = 1;
                pgbar.Maximum = PathFileName.Count();
                pgbar.Minimum = 0;
                string[] pathFileName = PathFileName;
                foreach (string text in pathFileName)
                {
                    status_msgbox.AppendText("Processing file .... " + Path.GetFileName(text));
                    if (signPdfFile(text, targetloc + "\\" + Path.GetFileName(text).ToString(), x509Certificate))
                    {
                        pgbar.PerformStep();
                        status_msgbox.AppendText("...... Document Signed.");
                        status_msgbox.AppendText(Environment.NewLine);
                    }
                    else
                    {
                        status_msgbox.AppendText(Environment.NewLine);
                        status_msgbox.AppendText("Document cannot be signed due to error.");
                        status_msgbox.AppendText(Environment.NewLine);
                    }
                }
            }
        }
        else
        {
            MessageBox.Show("This software is not activated either by trial or full or the Validity might have expired. Please close and re-open software and select Licensed certificate to Activate.");
            status_msgbox.AppendText(Environment.NewLine + "Validity : " + user_info[2] + Environment.NewLine + "This software is not activated either by trial or full. Please close and re-open software and select Licensed certificate to Activate." + Environment.NewLine);
        }
    }

    private void Browse_SF_Click(object sender, EventArgs e)
    {
        openFileDialog1.Multiselect = true;
        if (openFileDialog1.ShowDialog() == DialogResult.OK)
        {
            PathFileName = openFileDialog1.FileNames;
            string directoryName = Path.GetDirectoryName(PathFileName[0]);
            textBox1.Text = directoryName;
            status_msgbox.AppendText("Source files selected .... " + PathFileName.Length + " Files...");
            status_msgbox.AppendText(Environment.NewLine);
        }
        else
        {
            status_msgbox.AppendText("Source files not selected ...... Operation Aborted or Cancelled by user...");
            status_msgbox.AppendText(Environment.NewLine);
        }
    }

    private void Browse_TF_Click(object sender, EventArgs e)
    {
        if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
        {
            targetloc = folderBrowserDialog1.SelectedPath.ToString();
            textBox2.Text = targetloc;
            status_msgbox.AppendText("Target folder selected .... " + targetloc);
            status_msgbox.AppendText(Environment.NewLine);
        }
        else
        {
            status_msgbox.AppendText("Target folder not selected .... Operation cancelled by user");
            status_msgbox.AppendText(Environment.NewLine);
        }
    }

    private void sigLoc_listbox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sigLoc_listbox.SelectedItem.ToString() == "Last Page")
        {
            page_opt = "lastpage";
        }
    }

    private void Form1_Load(object sender, EventArgs e)
    {

    }
}
