using System.IO;
using System.Text.Json;

namespace FloatingHelper.Core.Configuration;

/// <summary>
/// 配置读写：JSON 持久化到用户数据目录；文件缺失用默认值，损坏则备份原文件后回退默认。
/// </summary>
public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>默认配置文件路径：%AppData%\FloatingHelper\settings.json</summary>
    public static string GetDefaultPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FloatingHelper",
            "settings.json");

    /// <summary>加载配置；文件不存在或损坏时返回默认配置。</summary>
    public static AppSettings Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings ?? new AppSettings();
        }
        catch
        {
            // 文件损坏：备份原文件后回退默认，避免反复读坏文件。
            TryBackup(path);
            return new AppSettings();
        }
    }

    /// <summary>保存配置到指定路径（自动创建目录）。</summary>
    public static void Save(AppSettings settings, string path)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static void TryBackup(string path)
    {
        try
        {
            var backup = $"{path}.bak-{DateTime.Now:yyyyMMddHHmmss}";
            File.Copy(path, backup, overwrite: true);
        }
        catch
        {
            // 备份失败不影响回退默认。
        }
    }
}
