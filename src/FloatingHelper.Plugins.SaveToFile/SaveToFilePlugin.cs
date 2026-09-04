using System.IO;
using System.Text.Json;
using System.Windows;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.SaveToFile;

/// <summary>
/// 「保存」插件：把选中文字追加写入用户指定的本地文本文档。
/// 用户在插件设置（主程序「设置」窗口 → 插件列表 → 保存 → 设置）里选择目标文档，
/// 配置持久化到 %AppData%\FloatingHelper\plugins\local.save.json，由插件自管，不依赖主程序。
/// </summary>
public sealed class SaveToFilePlugin : IPlugin
{
    private const string AppDataFolderName = "FloatingHelper";
    private const string PluginsFolderName = "plugins";
    private const string ConfigFileName = "local.save.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string Id => "local.save";

    public string Name => "保存";

    public string Icon => "\uE74E";

    public string Description => "将选中文字追加保存到指定的本地文本文档";

    public bool IsEnabled { get; set; } = true;

    /// <summary>本插件提供设置界面（选择要保存到的文本文档）。</summary>
    public bool HasSettings => true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>配置文件的默认位置（%AppData%\FloatingHelper\plugins\local.save.json）。</summary>
    public static string GetConfigPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDataFolderName,
            PluginsFolderName,
            ConfigFileName);

    /// <summary>读取配置；文件缺失或损坏时返回默认配置（未设置目标文件）。</summary>
    public static SaveToFileConfig LoadConfig(string? configPath = null)
    {
        var path = configPath ?? GetConfigPath();
        if (!File.Exists(path))
        {
            return new SaveToFileConfig();
        }

        try
        {
            return JsonSerializer.Deserialize<SaveToFileConfig>(File.ReadAllText(path), JsonOptions)
                ?? new SaveToFileConfig();
        }
        catch
        {
            return new SaveToFileConfig();
        }
    }

    /// <summary>保存配置（自动创建目录）。</summary>
    public static void SaveConfig(SaveToFileConfig config, string? configPath = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        var path = configPath ?? GetConfigPath();
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(config, JsonOptions));
    }

    /// <summary>把文本追加写入指定文件（文件不存在则自动创建，每段另起一行）。</summary>
    /// <returns>面向用户的成功描述。</returns>
    public static string AppendToFile(string filePath, string text)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.AppendAllText(filePath, text + Environment.NewLine);
        return $"已保存到 {Path.GetFileName(filePath)}";
    }

    /// <summary>执行一次保存动作，返回面向用户的结果描述；未设置目标文件时返回引导提示。</summary>
    public static string? Run(string selectedText, SaveToFileConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.TargetFilePath))
        {
            return "尚未设置保存文件，请先打开插件设置选择目标文本文档";
        }

        try
        {
            return AppendToFile(config.TargetFilePath, selectedText);
        }
        catch (Exception ex)
        {
            return $"保存失败：{ex.Message}";
        }
    }

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Run(context.SelectedText, LoadConfig()));
    }

    /// <summary>弹出设置窗口，让用户选择要保存到的文本文档；确定后持久化配置。</summary>
    public void ShowSettings(Window? owner = null)
    {
        var config = LoadConfig();
        var dialog = new SaveSettingsWindow(config.TargetFilePath)
        {
            Owner = owner,
        };

        if (dialog.ShowDialog() == true)
        {
            SaveConfig(new SaveToFileConfig { TargetFilePath = dialog.TargetFilePath });
        }
    }
}
