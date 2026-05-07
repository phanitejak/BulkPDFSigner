using System.Security.Cryptography.X509Certificates;

namespace BulkPdfSigner;

public partial class Form1 : Form
{
    private const int TrialBatchLimit = 5;
    private const string ContactBlurb =
        "This software is designed by Phaniteja K. Please contact or Whatsapp on +91 9848003093, " +
        "or Mail me at kondapalliphaniteja@gmail.com";

    private readonly LicenseClient _license = new();
    private X509Certificate2? _cert;
    private string _certUserName = "";
    private string _certSerial = "";
    private string[] _pathFileNames = Array.Empty<string>();
    private string _targetLoc = "";
    private bool _lastPage;

    public Form1()
    {
        InitializeComponent();

        Text = $"Bulk PDF Signer - Version {Application.ProductVersion.Split('+')[0]}";
        Shown += async (s, e) => await OnShownAsync();
        FormClosing += OnClosing;
        FormClosed += (s, e) => MessageBoxManager.Unregister();

        AppLogger.OnLine += line =>
        {
            if (!status_msgbox.IsHandleCreated) return;
            try
            {
                status_msgbox.BeginInvoke(new Action(() =>
                    status_msgbox.AppendText(line + Environment.NewLine)));
            }
            catch (InvalidOperationException) { /* form closing */ }
        };

        _license.LicenseRefreshed += info =>
            BeginInvoke(new Action(() => OnLicenseRefreshed(info)));
        _license.LicenseLost += reason =>
            BeginInvoke(new Action(() => OnLicenseLost(reason)));
    }

    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        _license.StopPolling();
        _license.Dispose();
        MessageBoxManager.Unregister();
    }

    // ---------- Startup ----------

    private async Task OnShownAsync()
    {
        AppLogger.Info("Activating Software...");

        var cert = SelectCertificate();
        if (cert is null)
        {
            AppLogger.Warn("No certificate selected.");
            MessageBox.Show("No certificate selected.");
            return;
        }
        _cert = cert;

        var (cn, serial) = GetCnAndSerial(cert);
        _certUserName = cn;
        if (string.IsNullOrWhiteSpace(serial))
        {
            AppLogger.Warn("Certificate has no SERIALNUMBER component; using common name as identifier.");
            serial = cn;
        }
        _certSerial = serial;

        // 1. Cache hit → use immediately, no popup, start polling.
        var cached = _license.TryLoadCache(serial);
        if (cached is not null)
        {
            AppLogger.Info($"License loaded from cache (valid till {cached.ValidTill}, type {cached.LicType}).");
            _license.StartPolling();
            return;
        }

        // 2. Cache miss → warn user about possible cold-start delay, then call server.
        MessageBox.Show(
            "Contacting licensing server. This can take up to 30 seconds the first time.",
            "Activating",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        AppLogger.Info("Contacting Licensing Server...");
        var (info, needTrial, error) = await _license.GetOrCreateAsync(serial, cn);

        if (info is null && needTrial)
        {
            AppLogger.Info($"No license for serial {serial}. Creating trial...");
            info = await _license.CreateTrialAsync(cn, serial);
            if (info is null)
            {
                MessageBox.Show("Unable to create a trial license. Please contact administrator.");
                return;
            }
        }
        else if (info is null)
        {
            AppLogger.Error(error ?? "License lookup failed.");
            MessageBox.Show(error ?? "License lookup failed.");
            return;
        }

        AnnounceLicense(info);
        _license.StartPolling();
    }

    private void AnnounceLicense(LicenseInfo info)
    {
        if (info.IsExpired)
        {
            AppLogger.Warn($"License expired on {info.ValidTill}.");
            MessageBox.Show($"This software is licensed till {info.ValidTill} only. Please contact administrator.");
            return;
        }

        var msg = info.IsTrial
            ? $"Trial license active. Up to {TrialBatchLimit} PDFs per batch. Valid till {info.ValidTill}."
            : $"License Found! Software Activated.\n\n" +
              $"Licensed to Mr. {info.Username} ({info.Circle} Circle)\nValid till: {info.ValidTill}";
        MessageBox.Show(msg, "Licensing Details");
        AppLogger.Info($"License active. Type: {info.LicType}, valid till {info.ValidTill}.");
    }

    // ---------- Polling callbacks ----------

    private void OnLicenseRefreshed(LicenseInfo info)
    {
        AppLogger.Info($"License refreshed (type {info.LicType}, valid till {info.ValidTill}).");
    }

    private void OnLicenseLost(string reason)
    {
        AppLogger.Warn(reason);
        MessageBox.Show(reason, "License", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    // ---------- Certificate ----------

    private static X509Certificate2? SelectCertificate()
    {
        using var store = new X509Store(StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);
        var collection = X509Certificate2UI.SelectFromCollection(
            store.Certificates, "Select certificate", "", X509SelectionFlag.SingleSelection);
        return collection.Count == 0 ? null : collection[0];
    }

    private static (string CN, string SerialNumber) GetCnAndSerial(X509Certificate2 cert)
    {
        string cn = cert.GetNameInfo(X509NameType.SimpleName, false);
        string serial = "";
        foreach (var part in cert.Subject.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("SERIALNUMBER=", StringComparison.OrdinalIgnoreCase))
            {
                serial = trimmed.Substring("SERIALNUMBER=".Length);
                break;
            }
        }
        return (cn, serial);
    }

    // ---------- UI handlers ----------

    private void aboutToolStrip_Click(object sender, EventArgs e)
    {
        var current = _license.Current;
        var version = Application.ProductVersion.Split('+')[0];

        if (current is null)
        {
            MessageBox.Show(
                $"{ContactBlurb}\n\nVersion: {version}\n\n" +
                "This software is not activated. Please close and re-open the software and select a Licensed certificate.",
                "About");
            return;
        }

        var body = current.IsTrial
            ? $"This is a Trial Version. Up to {TrialBatchLimit} PDFs can be signed per batch. Valid till {current.ValidTill}."
            : $"Licensed to Mr. {current.Username} of {current.Circle} Circle. Valid till {current.ValidTill}.";

        MessageBox.Show($"{ContactBlurb}\n\nVersion: {version}\n\n{body}", "Licensing Details");
    }

    private void beginsigning_Click_1(object sender, EventArgs e)
    {
        var info = _license.Current;
        if (_cert is null || info is null || info.IsExpired)
        {
            MessageBox.Show("Software is not activated or the license has expired. " +
                            "Please re-launch the application and select a Licensed certificate.");
            return;
        }

        if (_pathFileNames.Length == 0)
        {
            MessageBox.Show("No files selected.");
            AppLogger.Warn("No files selected.");
            return;
        }

        if (!ResolveTargetLocation())
        {
            return;
        }

        if (info.IsTrial && _pathFileNames.Length > TrialBatchLimit)
        {
            MessageBox.Show($"This is a Trial version. You cannot select more than {TrialBatchLimit} files in one batch.");
            return;
        }

        var useLastPage = _lastPage && info.AllowsLastPageStamp;

        pgbar.Step = 1;
        pgbar.Maximum = _pathFileNames.Length;
        pgbar.Minimum = 0;
        pgbar.Value = 0;

        foreach (var path in _pathFileNames)
        {
            var name = Path.GetFileName(path);
            AppLogger.Info($"Processing file ... {name}");
            var dest = Path.Combine(_targetLoc, name);
            try
            {
                PdfSigningService.Sign(path, dest, _cert, useLastPage);
                pgbar.PerformStep();
                AppLogger.Info($"... {name} signed.");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Sign failed for {name}: {ex.Message}");
            }
        }
    }

    private bool ResolveTargetLocation()
    {
        if (!string.IsNullOrEmpty(_targetLoc) && Directory.Exists(_targetLoc))
            return true;

        var typed = textBox2.Text;
        if (!string.IsNullOrEmpty(typed))
        {
            if (Directory.Exists(typed))
            {
                _targetLoc = typed;
                return true;
            }
            MessageBox.Show("The destination folder does not exist. Please pick a valid folder.");
            AppLogger.Warn($"Invalid target folder typed: {typed}");
            return false;
        }

        if (!string.IsNullOrEmpty(textBox1.Text))
        {
            _targetLoc = Path.Combine(textBox1.Text, "Result");
            try { Directory.CreateDirectory(_targetLoc); }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not create destination folder: {ex.Message}");
                AppLogger.Error($"Could not create {_targetLoc}: {ex.Message}");
                return false;
            }
            textBox2.Text = _targetLoc;
            return true;
        }

        MessageBox.Show("Please choose a destination folder.");
        return false;
    }

    private void Browse_SF_Click(object sender, EventArgs e)
    {
        openFileDialog1.Multiselect = true;
        openFileDialog1.Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*";
        if (openFileDialog1.ShowDialog() == DialogResult.OK)
        {
            _pathFileNames = openFileDialog1.FileNames;
            textBox1.Text = Path.GetDirectoryName(_pathFileNames[0]) ?? "";
            AppLogger.Info($"Source files selected ... {_pathFileNames.Length} files.");
        }
        else
        {
            AppLogger.Info("Source files not selected. Operation cancelled.");
        }
    }

    private void Browse_TF_Click(object sender, EventArgs e)
    {
        if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
        {
            _targetLoc = folderBrowserDialog1.SelectedPath;
            textBox2.Text = _targetLoc;
            AppLogger.Info($"Target folder selected: {_targetLoc}");
        }
        else
        {
            AppLogger.Info("Target folder not selected. Operation cancelled.");
        }
    }

    private void sigLoc_listbox_SelectedIndexChanged(object sender, EventArgs e)
    {
        _lastPage = sigLoc_listbox.SelectedItem?.ToString() == "Last Page";
    }
}
