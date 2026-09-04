using FloatingHelper.Core.Plugins;
using FloatingHelper.Plugins.SaveToFile;

namespace FloatingHelper.Core.Tests;

/// <summary>
/// 「保存」插件（local.save）的专项测试：
/// 元数据、配置读写、追加写入行为、未设置/失败分支。
/// 配置与写入均使用临时路径，避免污染真实用户目录。
/// </summary>
public class SaveToFilePluginTests
{
    private static PluginContext Ctx(string text) => new() { SelectedText = text };

    private static string TempDir() =>
        Path.Combine(Path.GetTempPath(), "FloatingHelper_Save_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveToFilePlugin_CanHandle_MeaningfulText_ShouldBeTrue()
    {
        Assert.True(new SaveToFilePlugin().CanHandle(Ctx("测试文字")));
    }

    [Fact]
    public void SaveToFilePlugin_CanHandle_Blank_ShouldBeFalse()
    {
        Assert.False(new SaveToFilePlugin().CanHandle(Ctx("   ")));
    }

    [Fact]
    public void SaveToFilePlugin_Metadata_ShouldBeSet()
    {
        var plugin = new SaveToFilePlugin();
        Assert.Equal("local.save", plugin.Id);
        Assert.Equal("保存", plugin.Name);
        Assert.True(plugin.HasSettings, "「保存」插件应提供设置界面（选择目标文档）。");
        Assert.True(plugin.IsEnabled);
        Assert.False(string.IsNullOrWhiteSpace(plugin.Description));
        Assert.False(string.IsNullOrWhiteSpace(plugin.Icon));
    }

    [Fact]
    public void SaveToFilePlugin_ConfigPath_ShouldBeUnderAppDataPlugins()
    {
        var path = SaveToFilePlugin.GetConfigPath();
        Assert.EndsWith(
            Path.Combine("FloatingHelper", "plugins", "local.save.json"),
            path,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SaveToFilePlugin_SaveAndLoadConfig_ShouldRoundTrip()
    {
        var dir = TempDir();
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "local.save.json");
            SaveToFilePlugin.SaveConfig(new SaveToFileConfig { TargetFilePath = @"C:\tmp\notes.txt" }, path);

            var loaded = SaveToFilePlugin.LoadConfig(path);
            Assert.Equal(@"C:\tmp\notes.txt", loaded.TargetFilePath);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SaveToFilePlugin_LoadConfig_MissingFile_ShouldReturnDefault()
    {
        var dir = TempDir();
        Directory.CreateDirectory(dir);
        try
        {
            var path = Path.Combine(dir, "missing.json");
            var loaded = SaveToFilePlugin.LoadConfig(path);
            Assert.Null(loaded.TargetFilePath);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SaveToFilePlugin_AppendToFile_ShouldAppendEachSaveOnNewLine()
    {
        var dir = TempDir();
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "notes.txt");
            SaveToFilePlugin.AppendToFile(file, "第一行");
            SaveToFilePlugin.AppendToFile(file, "第二行");

            var lines = File.ReadAllLines(file);
            Assert.Equal(2, lines.Length);
            Assert.Equal("第一行", lines[0]);
            Assert.Equal("第二行", lines[1]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SaveToFilePlugin_AppendToFile_ShouldCreateFileAndDirectories()
    {
        var dir = TempDir();
        try
        {
            var file = Path.Combine(dir, "sub", "deep", "notes.txt");
            var message = SaveToFilePlugin.AppendToFile(file, "首次内容");

            Assert.True(File.Exists(file));
            Assert.Equal("已保存到 notes.txt", message);
            Assert.Equal("首次内容", File.ReadAllText(file).TrimEnd('\r', '\n'));
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public void SaveToFilePlugin_Run_WithoutConfiguredFile_ShouldReturnPrompt()
    {
        var result = SaveToFilePlugin.Run("测试", new SaveToFileConfig());
        Assert.NotNull(result);
        Assert.Contains("尚未设置保存文件", result);
    }

    [Fact]
    public void SaveToFilePlugin_Run_WithConfiguredFile_ShouldAppendAndReport()
    {
        var dir = TempDir();
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "notes.txt");
            var result = SaveToFilePlugin.Run("测试内容", new SaveToFileConfig { TargetFilePath = file });

            Assert.Equal("已保存到 notes.txt", result);
            Assert.Contains("测试内容", File.ReadAllText(file));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SaveToFilePlugin_Run_WithInvalidTarget_ShouldReturnFailure()
    {
        var dir = TempDir();
        Directory.CreateDirectory(dir);
        try
        {
            // 同名目录会阻塞 File.AppendAllText，触发确定性的写失败分支。
            var blocked = Path.Combine(dir, "block.txt");
            Directory.CreateDirectory(blocked);

            var result = SaveToFilePlugin.Run("测试", new SaveToFileConfig { TargetFilePath = blocked });
            Assert.NotNull(result);
            Assert.Contains("保存失败", result);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
