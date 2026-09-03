using FloatingHelper.Core.Plugins;
using FloatingHelper.Plugins.Builtin;

namespace FloatingHelper.Core.Tests;

public class BuiltinPluginTests
{
    private static PluginContext Ctx(string text) => new() { SelectedText = text };

    [Theory]
    [InlineData("abc")]
    [InlineData("https://example.com")]
    public void CopyPlugin_CanHandle_MeaningfulText_ShouldBeTrue(string text)
    {
        Assert.True(new CopyPlugin().CanHandle(Ctx(text)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CopyPlugin_CanHandle_Blank_ShouldBeFalse(string text)
    {
        Assert.False(new CopyPlugin().CanHandle(Ctx(text)));
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("user@example.com")]
    public void SmartOpenPlugin_CanHandle_Recognizable_ShouldBeTrue(string text)
    {
        Assert.True(new SmartOpenPlugin().CanHandle(Ctx(text)));
    }

    [Theory]
    [InlineData("普通文本")]
    [InlineData("   ")]
    public void SmartOpenPlugin_CanHandle_PlainText_ShouldBeFalse(string text)
    {
        Assert.False(new SmartOpenPlugin().CanHandle(Ctx(text)));
    }

    [Fact]
    public void SmartOpenPlugin_CanHandle_ExistingDirectory_ShouldBeTrue()
    {
        Assert.True(new SmartOpenPlugin().CanHandle(Ctx(AppContext.BaseDirectory)));
    }

    [Fact]
    public void SearchPlugin_CanHandle_MeaningfulText_ShouldBeTrue()
    {
        Assert.True(new SearchPlugin().CanHandle(Ctx("任意文字")));
    }

    [Fact]
    public void SearchPlugin_CanHandle_Blank_ShouldBeFalse()
    {
        Assert.False(new SearchPlugin().CanHandle(Ctx("  ")));
    }

    [Fact]
    public void BuiltinPlugins_ShouldHaveUniqueIds()
    {
        IPlugin[] plugins = [new CopyPlugin(), new SmartOpenPlugin(), new SearchPlugin()];
        var ids = plugins.Select(p => p.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }
}
