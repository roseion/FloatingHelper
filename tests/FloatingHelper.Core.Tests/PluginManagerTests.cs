using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Core.Tests;

public class PluginManagerTests
{
    private sealed class FakePlugin : IPlugin
    {
        public required string Id { get; init; }
        public string Name => Id;
        public string Description => string.Empty;
        public bool IsEnabled { get; set; } = true;
        public Func<PluginContext, bool>? Matcher { get; init; }

        public bool CanHandle(PluginContext context) => Matcher?.Invoke(context) ?? true;

        public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(null);
    }

    private static PluginContext Ctx(string text = "x") => new() { SelectedText = text };

    [Fact]
    public void AddBuiltin_ShouldAddAndEnable()
    {
        var manager = new PluginManager();
        var plugin = new FakePlugin { Id = "a" };

        Assert.True(manager.AddBuiltin(plugin));
        Assert.True(plugin.IsEnabled);
        Assert.Single(manager.Plugins);
    }

    [Fact]
    public void AddBuiltin_DuplicateId_ShouldIgnore()
    {
        var manager = new PluginManager();
        manager.AddBuiltin(new FakePlugin { Id = "a" });

        Assert.False(manager.AddBuiltin(new FakePlugin { Id = "a" }));
        Assert.Single(manager.Plugins);
    }

    [Fact]
    public void GetApplicablePlugins_ShouldFilterDisabledAndNotMatched()
    {
        var manager = new PluginManager();
        var always = new FakePlugin { Id = "always" };
        var never = new FakePlugin { Id = "never", Matcher = _ => false };
        var disabled = new FakePlugin { Id = "disabled" };
        manager.AddBuiltin(always);
        manager.AddBuiltin(never);
        manager.AddBuiltin(disabled);
        manager.SetEnabled(disabled, false);

        var applicable = manager.GetApplicablePlugins(Ctx());
        var item = Assert.Single(applicable);
        Assert.Equal("always", item.Id);
    }

    [Fact]
    public void GetApplicablePlugins_DisabledPlugin_HasNoEffectOnOthers()
    {
        var manager = new PluginManager();
        var a = new FakePlugin { Id = "a" };
        var b = new FakePlugin { Id = "b" };
        manager.AddBuiltin(a);
        manager.AddBuiltin(b);
        manager.SetEnabled(a, false);

        var applicable = manager.GetApplicablePlugins(Ctx());
        var item = Assert.Single(applicable);
        Assert.Equal("b", item.Id);
    }

    [Fact]
    public void Unload_ShouldRemoveAndDisable()
    {
        var manager = new PluginManager();
        var plugin = new FakePlugin { Id = "a" };
        manager.AddBuiltin(plugin);

        Assert.True(manager.Unload(plugin));
        Assert.False(plugin.IsEnabled);
        Assert.Empty(manager.Plugins);
        Assert.False(manager.Unload(plugin));
    }

    [Fact]
    public void LoadFromDirectory_MissingDirectory_ShouldReturnZero()
    {
        var manager = new PluginManager();
        var path = Path.Combine(Path.GetTempPath(), "NoSuchPlugins_123456");
        Assert.Equal(0, manager.LoadFromDirectory(path));
    }
}
