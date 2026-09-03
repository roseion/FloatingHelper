using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Configuration;

namespace FloatingHelper.Core.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly string _path;

    public SettingsStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "FloatingHelperTests_" + Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_dir, "settings.json");
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch
        {
            // 清理失败忽略。
        }
    }

    [Fact]
    public void Load_MissingFile_ShouldReturnDefault()
    {
        var settings = SettingsStore.Load(_path);

        Assert.Equal(SearchUrlBuilder.DefaultSearchTemplate, settings.SearchTemplate);
        Assert.False(settings.AutoStart);
        Assert.Empty(settings.Plugins);
        Assert.Equal(AppSettings.CurrentVersion, settings.Version);
        Assert.Equal(ToolbarDisplayMode.IconAndText, settings.DisplayMode);
    }

    [Fact]
    public void Save_ThenLoad_ShouldRoundTrip()
    {
        var original = new AppSettings
        {
            SearchTemplate = "https://example.com/search?q={0}",
            AutoStart = true,
            DisplayMode = ToolbarDisplayMode.IconOnly,
        };
        original.Plugins["builtin.copy"] = new PluginSetting { Enabled = true };
        original.Plugins["external.demo"] = new PluginSetting { Enabled = false, Path = @"C:\demo\demo.dll" };

        SettingsStore.Save(original, _path);
        var loaded = SettingsStore.Load(_path);

        Assert.Equal(original.SearchTemplate, loaded.SearchTemplate);
        Assert.True(loaded.AutoStart);
        Assert.Equal(ToolbarDisplayMode.IconOnly, loaded.DisplayMode);
        Assert.True(loaded.Plugins["builtin.copy"].Enabled);
        Assert.False(loaded.Plugins["external.demo"].Enabled);
        Assert.Equal(@"C:\demo\demo.dll", loaded.Plugins["external.demo"].Path);
    }

    [Fact]
    public void Save_DisplayMode_EachVariant_ShouldRoundTrip()
    {
        foreach (var mode in new[] { ToolbarDisplayMode.IconAndText, ToolbarDisplayMode.IconOnly, ToolbarDisplayMode.TextOnly })
        {
            var original = new AppSettings { DisplayMode = mode };
            SettingsStore.Save(original, _path);
            var loaded = SettingsStore.Load(_path);
            Assert.Equal(mode, loaded.DisplayMode);
        }
    }

    [Fact]
    public void Save_ShouldCreateDirectory()
    {
        var nested = Path.Combine(_dir, "a", "b", "settings.json");
        SettingsStore.Save(new AppSettings(), nested);
        Assert.True(File.Exists(nested));
    }

    [Fact]
    public void Load_CorruptFile_ShouldReturnDefaultAndBackup()
    {
        File.WriteAllText(_path, "{ invalid json !!!");
        var settings = SettingsStore.Load(_path);

        Assert.Equal(SearchUrlBuilder.DefaultSearchTemplate, settings.SearchTemplate);
        Assert.True(Directory.EnumerateFiles(_dir, "settings.json.bak-*").Any(), "损坏文件应被备份为 bak-*");
    }

    [Fact]
    public void Load_EmptyJson_ShouldReturnDefault()
    {
        File.WriteAllText(_path, "null");
        var settings = SettingsStore.Load(_path);

        Assert.Equal(SearchUrlBuilder.DefaultSearchTemplate, settings.SearchTemplate);
    }

    [Fact]
    public void GetDefaultPath_ShouldPointToAppDataFloatingHelper()
    {
        var path = SettingsStore.GetDefaultPath();
        Assert.EndsWith(Path.Combine("FloatingHelper", "settings.json"), path, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AppData", path, StringComparison.OrdinalIgnoreCase);
    }
}
