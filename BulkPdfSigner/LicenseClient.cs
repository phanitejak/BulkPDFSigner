using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace BulkPdfSigner;

public sealed class LicenseClient : IDisposable
{
    private const string ApiBase = "https://bulk-pdf-signer-license-provider-496807224907.asia-south1.run.app";
    private static readonly string ApiKey = ResolveApiKey();

    private static string ResolveApiKey()
    {
        var fromAssembly = typeof(LicenseClient).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ApiKey")?.Value;
        if (!string.IsNullOrEmpty(fromAssembly)) return fromAssembly;
        return Environment.GetEnvironmentVariable("BULK_PDF_SIGNER_API_KEY") ?? "";
    }

    private static readonly TimeSpan CacheTtl     = TimeSpan.FromHours(24);
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    private readonly HttpClient _http;
    private readonly string _cachePath;
    private readonly System.Windows.Forms.Timer _poll;

    private string? _serial;
    private string? _fallbackUser;
    private LicenseInfo? _current;

    public event Action<LicenseInfo>? LicenseRefreshed;
    public event Action<string>? LicenseLost;

    public LicenseClient()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.Add("X-API-KEY", ApiKey);

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BulkPdfSigner");
        Directory.CreateDirectory(dir);
        _cachePath = Path.Combine(dir, "license.json");

        _poll = new System.Windows.Forms.Timer
        {
            Interval = (int)PollInterval.TotalMilliseconds
        };
        _poll.Tick += async (_, _) => await PollAsync();
    }

    public LicenseInfo? Current => _current;

    public LicenseInfo? TryLoadCache(string serial)
    {
        if (!File.Exists(_cachePath)) return null;
        try
        {
            var json = File.ReadAllText(_cachePath);
            var entry = JsonSerializer.Deserialize<CacheEntry>(json);
            if (entry is null || entry.Info is null) return null;
            if (!string.Equals(entry.Info.UsbSerial, serial, StringComparison.OrdinalIgnoreCase))
            {
                AppLogger.Info("Cached license is for a different certificate; ignoring.");
                return null;
            }
            if (DateTime.UtcNow - entry.FetchedAtUtc > CacheTtl)
            {
                AppLogger.Info("License cache is older than 24 hours; refetching from server.");
                return null;
            }
            if (entry.Info.IsExpired)
            {
                AppLogger.Info("Cached license is expired; refetching from server.");
                return null;
            }
            _current = entry.Info;
            return entry.Info;
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Failed to read license cache: {ex.Message}");
            return null;
        }
    }

    public async Task<(LicenseInfo? info, bool needTrial, string? error)> GetOrCreateAsync(
        string serial,
        string fallbackUser)
    {
        _serial = serial;
        _fallbackUser = fallbackUser;

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                var info = await GetAsync(serial);
                if (info is not null)
                {
                    _current = info;
                    SaveCache(info);
                    return (info, false, null);
                }
                return (null, true, null);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                AppLogger.Warn($"License GET attempt {attempt} failed: {ex.Message}");
                if (attempt == 1) await Task.Delay(2000);
            }
        }
        return (null, false, "Could not contact the licensing server. Please check your internet connection and try again.");
    }

    public async Task<LicenseInfo?> CreateTrialAsync(string user, string serial)
    {
        var url = $"{ApiBase}/license";
        var payload = new
        {
            username   = user,
            usb_serial = serial,
            circle     = "Trial",
            lic_type   = "Trial"
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        try
        {
            using var resp = await _http.PostAsync(url, content);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                AppLogger.Error($"Trial creation failed ({(int)resp.StatusCode}): {err}");
                return null;
            }
            var info = await GetAsync(serial);
            if (info is not null)
            {
                _current = info;
                SaveCache(info);
            }
            return info;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Trial creation network error: {ex.Message}");
            return null;
        }
    }

    public void StartPolling() => _poll.Start();
    public void StopPolling()  => _poll.Stop();

    public void ClearCache()
    {
        try
        {
            if (File.Exists(_cachePath)) File.Delete(_cachePath);
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Failed to clear license cache: {ex.Message}");
        }
    }

    private async Task PollAsync()
    {
        if (_serial is null) return;
        try
        {
            var info = await GetAsync(_serial);
            if (info is null)
            {
                AppLogger.Warn("Server reports no license for this certificate (revoked).");
                ClearCache();
                _current = null;
                LicenseLost?.Invoke("Your license has been revoked. The app will switch to Trial mode.");

                if (_fallbackUser is not null)
                {
                    var trial = await CreateTrialAsync(_fallbackUser, _serial);
                    if (trial is not null) LicenseRefreshed?.Invoke(trial);
                }
                return;
            }

            _current = info;
            SaveCache(info);

            if (info.IsExpired)
            {
                AppLogger.Warn($"License has expired ({info.ValidTill}).");
                ClearCache();
                LicenseLost?.Invoke($"Your license expired on {info.ValidTill}. Please contact administrator.");
            }
            else
            {
                LicenseRefreshed?.Invoke(info);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Background license poll failed: {ex.Message}");
        }
    }

    private async Task<LicenseInfo?> GetAsync(string serial)
    {
        var url = $"{ApiBase}/license?serialnum={Uri.EscapeDataString(serial)}";
        using var resp = await _http.GetAsync(url);
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        return new LicenseInfo(
            Username:  root.TryGetProperty("username",   out var u) ? u.GetString() ?? "" : "",
            UsbSerial: root.TryGetProperty("usb_serial", out var s) ? s.GetString() ?? serial : serial,
            Circle:    root.TryGetProperty("circle",     out var c) ? c.GetString() ?? "" : "",
            ValidTill: root.TryGetProperty("valid_till", out var v) ? v.GetString() ?? "" : "",
            LicType:  (root.TryGetProperty("lic_type",   out var t) ? t.GetString() ?? "" : "")
                       .Trim().ToUpperInvariant());
    }

    private void SaveCache(LicenseInfo info)
    {
        try
        {
            var json = JsonSerializer.Serialize(new CacheEntry(info, DateTime.UtcNow));
            File.WriteAllText(_cachePath, json);
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Failed to save license cache: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _poll.Stop();
        _poll.Dispose();
        _http.Dispose();
    }

    private sealed record CacheEntry(LicenseInfo? Info, DateTime FetchedAtUtc);
}
