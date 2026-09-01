using System.IO;

namespace FloatingHelper.Core.Logging;

/// <summary>
/// 极简文件日志：写入 %AppData%\FloatingHelper\logs\yyyyMMdd.log，用于稳定性排查。
/// 日志写入失败不影响主程序。
/// </summary>
public static class Logger
{
    private static readonly object Lock = new();
    private static string? _logDirectory;

    /// <summary>确保日志目录已创建。首次调用时初始化。</summary>
    public static void EnsureInitialized()
    {
        if (_logDirectory is not null)
        {
            return;
        }

        _logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FloatingHelper",
            "logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message}: {ex.GetType().Name} {ex.Message}");

    private static void Write(string level, string message)
    {
        try
        {
            EnsureInitialized();
            var path = Path.Combine(_logDirectory!, DateTime.Now.ToString("yyyyMMdd") + ".log");
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}";
            lock (Lock)
            {
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch
        {
            // 日志失败不影响主程序。
        }
    }
}
