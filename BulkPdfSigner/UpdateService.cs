using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace BulkPdfSigner;

public sealed class UpdateService : IDisposable
{
    private const string ReleasesApi =
        "https://api.github.com/repos/phanitejak/BulkPDFSigner/releases/latest";

    private readonly HttpClient _http;

    public UpdateService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _http.DefaultRequestHeaders.Add("User-Agent", "BulkPdfSigner-Updater");
        _http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        try
        {
            using var resp = await _http.GetAsync(ReleasesApi);
            if (!resp.IsSuccessStatusCode)
            {
                AppLogger.Warn($"Update check returned HTTP {(int)resp.StatusCode}");
                return null;
            }

            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var newVersionStr = tag.TrimStart('v', 'V');
            if (!Version.TryParse(newVersionStr, out var newVersion))
            {
                AppLogger.Warn($"Could not parse latest version from tag '{tag}'.");
                return null;
            }

            var currentStr = Application.ProductVersion.Split('+')[0];
            if (!Version.TryParse(currentStr, out var currentVersion))
            {
                AppLogger.Warn($"Could not parse current version '{currentStr}'.");
                return null;
            }

            if (newVersion <= currentVersion)
            {
                AppLogger.Info($"App is up-to-date (current {currentVersion}, latest {newVersion}).");
                return null;
            }

            string? exeUrl = null;
            string? shaUrl = null;
            foreach (var asset in root.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                var url = asset.GetProperty("browser_download_url").GetString() ?? "";
                if (name.EndsWith(".exe.sha256", StringComparison.OrdinalIgnoreCase)) shaUrl = url;
                else if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) exeUrl = url;
            }
            if (exeUrl is null)
            {
                AppLogger.Warn("Latest release has no .exe asset attached.");
                return null;
            }

            var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            return new UpdateInfo(newVersion, currentVersion, exeUrl, shaUrl, notes);
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Update check failed: {ex.Message}");
            return null;
        }
    }

    public async Task ApplyUpdateAsync(UpdateInfo info, IProgress<double>? progress = null)
    {
        var currentExe = Environment.ProcessPath
            ?? throw new InvalidOperationException("Cannot determine current executable path.");
        var dir = Path.GetDirectoryName(currentExe)
            ?? throw new InvalidOperationException("Cannot determine application directory.");
        var newExeStaging = Path.Combine(dir, "BulkPdfSigner.exe.new");
        var oldBackup = currentExe + ".old";

        AppLogger.Info($"Downloading update {info.NewVersion}...");
        await DownloadAsync(info.DownloadUrl, newExeStaging, progress);

        if (info.ChecksumUrl is not null)
        {
            AppLogger.Info("Verifying download...");
            var expected = await DownloadChecksumAsync(info.ChecksumUrl);
            var actual = await ComputeSha256Async(newExeStaging);
            if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(newExeStaging);
                throw new InvalidOperationException(
                    $"Downloaded update failed SHA256 verification. Expected {expected}, got {actual}.");
            }
            AppLogger.Info("Checksum verified.");
        }

        TryDelete(oldBackup);
        File.Move(currentExe, oldBackup);
        try
        {
            File.Move(newExeStaging, currentExe);
        }
        catch
        {
            // Restore the original if we can't put the new one in place.
            File.Move(oldBackup, currentExe);
            throw;
        }

        AppLogger.Info("Update installed. Restarting...");
        Process.Start(new ProcessStartInfo
        {
            FileName = currentExe,
            UseShellExecute = true
        });
        Application.Exit();
    }

    public static void CleanupOldBackup()
    {
        try
        {
            var currentExe = Environment.ProcessPath;
            if (currentExe is null) return;
            var dir = Path.GetDirectoryName(currentExe);
            if (dir is null || !Directory.Exists(dir)) return;
            foreach (var stale in Directory.EnumerateFiles(dir, "*.exe.old"))
            {
                try { File.Delete(stale); }
                catch { /* will retry on next launch */ }
            }
        }
        catch { }
    }

    private async Task DownloadAsync(string url, string destPath, IProgress<double>? progress)
    {
        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();

        var total = resp.Content.Headers.ContentLength ?? 0L;
        using var stream = await resp.Content.ReadAsStreamAsync();
        using var dest = File.Create(destPath);
        var buffer = new byte[81920];
        long copied = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await dest.WriteAsync(buffer.AsMemory(0, read));
            copied += read;
            if (total > 0) progress?.Report((double)copied / total);
        }
    }

    private async Task<string> DownloadChecksumAsync(string url)
    {
        var content = await _http.GetStringAsync(url);
        var first = content.Split(
            new[] { ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first ?? "";
    }

    private static async Task<string> ComputeSha256Async(string path)
    {
        using var sha = SHA256.Create();
        using var fs = File.OpenRead(path);
        var hash = await sha.ComputeHashAsync(fs);
        return Convert.ToHexString(hash);
    }

    private static void TryDelete(string path)
    {
        if (!File.Exists(path)) return;
        try { File.Delete(path); }
        catch { /* may be locked by another instance; will retry next launch */ }
    }

    public void Dispose() => _http.Dispose();
}

public sealed record UpdateInfo(
    Version NewVersion,
    Version CurrentVersion,
    string DownloadUrl,
    string? ChecksumUrl,
    string ReleaseNotes);
