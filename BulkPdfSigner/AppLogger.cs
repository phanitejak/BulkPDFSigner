namespace BulkPdfSigner;

public static class AppLogger
{
    private static readonly object _lock = new();
    private static readonly string _logPath;

    public static event Action<string>? OnLine;

    static AppLogger()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BulkPdfSigner");
        Directory.CreateDirectory(dir);
        _logPath = Path.Combine(dir, "log.txt");
    }

    public static string LogPath => _logPath;

    public static void Info(string msg) => Write("INFO", msg);
    public static void Warn(string msg) => Write("WARN", msg);
    public static void Error(string msg) => Write("ERROR", msg);

    private static void Write(string level, string msg)
    {
        var stamped = $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss} [{level}] {msg}";
        try
        {
            lock (_lock)
            {
                File.AppendAllText(_logPath, stamped + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never throw.
        }
        OnLine?.Invoke(msg);
    }
}
