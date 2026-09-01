using FloatingHelper.Core.Plugins;
using FloatingHelper.Plugins.Builtin;

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

    /// <summary>通过 LoadFromFile 加载 Builtin 程序集，返回首个外部插件。</summary>
    private static (PluginManager Manager, IPlugin External) LoadExternalFromBuiltin()
    {
        var manager = new PluginManager();
        var dll = typeof(CopyPlugin).Assembly.Location;
        Assert.True(manager.LoadFromFile(dll) > 0, "Builtin DLL 应能加载出插件");
        var external = manager.Plugins.First(p => !manager.IsBuiltin(p));
        return (manager, external);
    }

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
    public void Unload_Builtin_ShouldReturnFalseAndKeepEnabled()
    {
        var manager = new PluginManager();
        var plugin = new FakePlugin { Id = "a" };
        manager.AddBuiltin(plugin);

        // 内置插件不可卸载，仅可启停。
        Assert.False(manager.Unload(plugin));
        Assert.Single(manager.Plugins);
        Assert.True(plugin.IsEnabled);
    }

    [Fact]
    public void Unload_ExternalPlugin_ShouldRemoveAndDisable()
    {
        var (manager, external) = LoadExternalFromBuiltin();

        Assert.True(manager.Unload(external));
        Assert.False(external.IsEnabled);
        Assert.DoesNotContain(external, manager.Plugins);
        Assert.False(manager.Unload(external));
    }

    [Fact]
    public void LoadFromDirectory_MissingDirectory_ShouldReturnZero()
    {
        var manager = new PluginManager();
        var path = Path.Combine(Path.GetTempPath(), "NoSuchPlugins_123456");
        Assert.Equal(0, manager.LoadFromDirectory(path));
    }

    [Fact]
    public void LoadFromFile_MissingFile_ShouldReturnZero()
    {
        var manager = new PluginManager();
        Assert.Equal(0, manager.LoadFromFile(@"C:\NoSuchPlugin_123456\a.dll"));
    }

    [Fact]
    public void LoadFromFile_ValidPlugin_ShouldLoadAndMarkExternal()
    {
        var (manager, external) = LoadExternalFromBuiltin();

        Assert.True(manager.IsBuiltin(external) == false);
        Assert.Equal(typeof(CopyPlugin).Assembly.Location, manager.GetExternalPath(external));
        Assert.Same(external, manager.GetPlugin(external.Id));
    }

    [Fact]
    public void StateChanged_ShouldFireOnAddAndEnableToggle()
    {
        var manager = new PluginManager();
        var count = 0;
        manager.StateChanged += (_, _) => count++;

        var plugin = new FakePlugin { Id = "a" };
        manager.AddBuiltin(plugin);          // 新增 +1
        manager.SetEnabled(plugin, false);   // 启停 +1
        Assert.Equal(2, count);
    }

    [Fact]
    public void StateChanged_ShouldNotFireOnIdempotentEnable()
    {
        var manager = new PluginManager();
        var count = 0;
        manager.StateChanged += (_, _) => count++;

        var plugin = new FakePlugin { Id = "a" };
        manager.AddBuiltin(plugin);          // 新增 +1
        manager.SetEnabled(plugin, true);    // 状态未变，不触发
        Assert.Equal(1, count);
    }

    [Fact]
    public void GetPlugin_UnknownId_ShouldReturnNull()
    {
        var manager = new PluginManager();
        Assert.Null(manager.GetPlugin("not-exist"));
    }
}
