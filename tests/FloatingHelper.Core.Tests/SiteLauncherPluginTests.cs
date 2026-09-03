using FloatingHelper.Core.Plugins;
using FloatingHelper.Plugins.SiteLauncher;

namespace FloatingHelper.Core.Tests;

/// <summary>
/// 站点直达插件（SiteLauncher 外部插件包）测试。
/// 该插件包不注册进主程序，通过 PluginManager.LoadFromFile 从 DLL 自动发现，
/// 因此测试重点验证：URL 构造正确 + 插件管理器能独立加载 / 启停 / 卸载全部六个插件。
/// </summary>
public class SiteLauncherPluginTests
{
    private static PluginContext Ctx(string text) => new() { SelectedText = text };

    private static readonly IPlugin[] AllPlugins =
    [
        new XiaohongshuSearchPlugin(),
        new ZhihuSearchPlugin(),
        new WeiboSearchPlugin(),
        new DoubaoAskPlugin(),
        new YuanbaoAskPlugin(),
        new DeepSeekAskPlugin(),
    ];

    [Theory]
    [InlineData("小红书文案")]
    [InlineData("https://example.com")]
    public void XiaohongshuSearchPlugin_CanHandle_MeaningfulText_ShouldBeTrue(string text)
    {
        Assert.True(new XiaohongshuSearchPlugin().CanHandle(Ctx(text)));
    }

    [Fact]
    public void XiaohongshuSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new XiaohongshuSearchPlugin().BuildUrl("测试 小红书");
        Assert.StartsWith("https://www.xiaohongshu.com/search_result?keyword=", url);
        Assert.Contains("source=web_explore_feed", url);
        Assert.DoesNotContain(" ", url);
    }

    [Fact]
    public void ZhihuSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new ZhihuSearchPlugin().BuildUrl("hello world");
        Assert.Equal("https://www.zhihu.com/search?type=content&q=hello%20world", url);
    }

    [Fact]
    public void WeiboSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new WeiboSearchPlugin().BuildUrl("热点 事件");
        Assert.StartsWith("https://s.weibo.com/weibo?q=", url);
        Assert.DoesNotContain(" ", url);
    }

    [Fact]
    public void DoubaoAskPlugin_BuildUrl_ShouldBuildUrlActionWithEscapedJson()
    {
        var url = new DoubaoAskPlugin().BuildUrl("测试\"提问");
        const string prefix = "https://www.doubao.com/chat/url-action?action=";
        Assert.StartsWith(prefix, url);

        // 解码 action 参数后应为 Send_Message 协议 JSON，text 与原文一致（引号已转义）。
        var action = Uri.UnescapeDataString(url[prefix.Length..]);
        Assert.Contains("\"pluginId\":\"Send_Message\"", action);
        Assert.Contains("\"payload\":{\"text\":\"测试\\\"提问\"}", action);
    }

    [Fact]
    public void DeepSeekAskPlugin_BuildUrl_ShouldEncodeQuestion()
    {
        var url = new DeepSeekAskPlugin().BuildUrl("什么是 YOLO");
        Assert.Equal("https://chat.deepseek.com/a/chat?q=%E4%BB%80%E4%B9%88%E6%98%AF%20YOLO", url);
    }

    [Fact]
    public void YuanbaoAskPlugin_BuildUrl_ShouldPointToChatPage()
    {
        var url = new YuanbaoAskPlugin().BuildUrl("任意文本");
        Assert.Equal("https://yuanbao.tencent.com/chat/", url);
    }

    [Fact]
    public void AllSitePlugins_CanHandle_Blank_ShouldBeFalse()
    {
        Assert.All(AllPlugins, p => Assert.False(p.CanHandle(Ctx("   "))));
    }

    [Fact]
    public void AllSitePlugins_ShouldHaveUniqueIds()
    {
        var ids = AllPlugins.Select(p => p.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void AllSitePlugins_ShouldBeEnabledByDefault()
    {
        Assert.All(AllPlugins, p => Assert.True(p.IsEnabled));
    }

    /// <summary>
    /// 核心验收点：不修改主程序、不做任何注册，仅把插件 DLL 交给 PluginManager，
    /// 即可自动发现全部六个站点插件，且每个插件可独立启停与卸载。
    /// </summary>
    [Fact]
    public void PluginManager_LoadFromFile_ShouldDiscoverAllSixSitePlugins()
    {
        var manager = new PluginManager();
        var dll = typeof(XiaohongshuSearchPlugin).Assembly.Location;

        var loaded = manager.LoadFromFile(dll);

        Assert.Equal(6, loaded);
        var sitePlugins = manager.Plugins.Where(p => p.Id.StartsWith("site.", StringComparison.Ordinal)).ToList();
        Assert.Equal(6, sitePlugins.Count);

        // 全部按外部插件管理（可卸载），且 Id 唯一。
        Assert.All(sitePlugins, p => Assert.False(manager.IsBuiltin(p)));
        Assert.Equal(6, sitePlugins.Select(p => p.Id).Distinct().Count());

        // 加载后即可适配选区，出现在工具栏。
        Assert.Equal(6, manager.GetApplicablePlugins(Ctx("测试文字")).Count);

        // 单个插件可独立卸载，其余不受影响。
        var first = sitePlugins[0];
        Assert.True(manager.Unload(first));
        Assert.DoesNotContain(first, manager.Plugins);
        Assert.Equal(5, manager.Plugins.Count);
    }
}
