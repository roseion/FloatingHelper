using FloatingHelper.Core.Plugins;
using FloatingHelper.Plugins.Builtin;

namespace FloatingHelper.Core.Tests;

public class TranslatePluginTests
{
    private readonly TranslatePlugin _plugin = new();

    [Fact]
    public void CanHandle_WithNonEmptyText_ReturnsTrue()
    {
        var context = new PluginContext { SelectedText = "Hello world" };
        Assert.True(_plugin.CanHandle(context));
    }

    [Fact]
    public void CanHandle_WithEmptyText_ReturnsFalse()
    {
        var context = new PluginContext { SelectedText = "" };
        Assert.False(_plugin.CanHandle(context));
    }

    [Fact]
    public void CanHandle_WithWhitespace_ReturnsFalse()
    {
        var context = new PluginContext { SelectedText = "   " };
        Assert.False(_plugin.CanHandle(context));
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyText_ReturnsNull()
    {
        var context = new PluginContext { SelectedText = "   " };
        var result = await _plugin.ExecuteAsync(context);
        Assert.Null(result);
    }

    [Fact]
    public void Id_Name_Description_AreValid()
    {
        Assert.Equal("builtin.translate", _plugin.Id);
        Assert.Equal("翻译", _plugin.Name);
        Assert.False(string.IsNullOrWhiteSpace(_plugin.Description));
    }
}
