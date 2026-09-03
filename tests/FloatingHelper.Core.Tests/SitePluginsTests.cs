using FloatingHelper.Core.Plugins;
using FloatingHelper.Plugins.AmapSearch;
using FloatingHelper.Plugins.BaiduMapSearch;
using FloatingHelper.Plugins.BaiduSearch;
using FloatingHelper.Plugins.BingSearch;
using FloatingHelper.Plugins.DeepSeekAsk;
using FloatingHelper.Plugins.DoubaoAsk;
using FloatingHelper.Plugins.GoogleSearch;
using FloatingHelper.Plugins.WeiboSearch;
using FloatingHelper.Plugins.XiaohongshuSearch;
using FloatingHelper.Plugins.YuanbaoAsk;
using FloatingHelper.Plugins.ZhihuSearch;

namespace FloatingHelper.Core.Tests;

/// <summary>
/// 十一个站点直达插件的测试。每个插件是一个独立的安装包（独立项目、独立 DLL），
/// 因此测试重点验证：
///   1) URL 构造正确；
///   2) 每个 DLL 恰好包含一个插件，可被 PluginManager 单独加载 / 启停 / 卸载（不改主程序）。
/// </summary>
public class SitePluginsTests
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
        new BaiduSearchPlugin(),
        new GoogleSearchPlugin(),
        new BingSearchPlugin(),
        new AmapSearchPlugin(),
        new BaiduMapSearchPlugin(),
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
    public void DeepSeekAskPlugin_BuildUrl_ShouldPointToChatPage()
    {
        var url = new DeepSeekAskPlugin().BuildUrl("什么是 YOLO");
        Assert.Equal("https://chat.deepseek.com/a/chat", url);
    }

    [Fact]
    public void YuanbaoAskPlugin_BuildUrl_ShouldPointToChatPage()
    {
        var url = new YuanbaoAskPlugin().BuildUrl("任意文本");
        Assert.Equal("https://yuanbao.tencent.com/chat/", url);
    }

    [Fact]
    public void BaiduSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new BaiduSearchPlugin().BuildUrl("测试 百度");
        Assert.StartsWith("https://www.baidu.com/s?wd=", url);
        Assert.DoesNotContain(" ", url);
    }

    [Fact]
    public void GoogleSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new GoogleSearchPlugin().BuildUrl("hello world");
        Assert.Equal("https://www.google.com/search?q=hello%20world", url);
    }

    [Fact]
    public void BingSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new BingSearchPlugin().BuildUrl("热点 事件");
        Assert.StartsWith("https://www.bing.com/search?q=", url);
        Assert.DoesNotContain(" ", url);
    }

    [Fact]
    public void AmapSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new AmapSearchPlugin().BuildUrl("广州 美食");
        Assert.StartsWith("https://www.amap.com/search?query=", url);
        Assert.DoesNotContain(" ", url);
    }

    [Fact]
    public void BaiduMapSearchPlugin_BuildUrl_ShouldEncodePathAndPrefillKeyword()
    {
        var url = new BaiduMapSearchPlugin().BuildUrl("广州塔");
        Assert.Equal(
            "https://map.baidu.com/search/%E5%B9%BF%E5%B7%9E%E5%A1%94?querytype=s&wd=%E5%B9%BF%E5%B7%9E%E5%A1%94",
            url);
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
    /// 核心验收点：十一个插件是十一个独立安装包（独立 DLL）。
    /// 不修改主程序、不做任何注册，仅把某个插件 DLL 交给 PluginManager，
    /// 应恰好发现 1 个插件，可独立启停与卸载，与其他插件互不影响。
    /// </summary>
    [Fact]
    public void PluginManager_LoadFromFile_EachDll_ShouldDiscoverExactlyOnePlugin()
    {
        foreach (var plugin in AllPlugins)
        {
            var manager = new PluginManager();
            var dll = plugin.GetType().Assembly.Location;

            var loaded = manager.LoadFromFile(dll);

            Assert.Equal(1, loaded);
            var discovered = Assert.Single(manager.Plugins);
            Assert.Equal(plugin.Id, discovered.Id);
            Assert.False(manager.IsBuiltin(discovered));
            Assert.True(discovered.IsEnabled);

            // 加载后即可适配选区，出现在工具栏。
            Assert.Single(manager.GetApplicablePlugins(Ctx("测试文字")));

            // 单个插件可独立卸载，卸载后列表为空。
            Assert.True(manager.Unload(discovered));
            Assert.Empty(manager.Plugins);
        }
    }

    /// <summary>
    /// 所有独立插件 DLL 可同时放入 plugins/ 目录，一次性全部发现（模拟真实安装场景）。
    /// </summary>
    [Fact]
    public void PluginManager_LoadFromDirectory_ShouldDiscoverAllDlls()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "FloatingHelper_SitePlugins_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            // 把所有插件 DLL 复制到一个目录，模拟用户把所有安装包放进 plugins/。
            foreach (var plugin in AllPlugins)
            {
                var dll = plugin.GetType().Assembly.Location;
                File.Copy(dll, Path.Combine(tempDir, Path.GetFileName(dll)));
            }

            var manager = new PluginManager();
            var loaded = manager.LoadFromDirectory(tempDir);

            Assert.Equal(AllPlugins.Length, loaded);
            var sitePlugins = manager.Plugins.Where(p => p.Id.StartsWith("site.", StringComparison.Ordinal)).ToList();
            Assert.Equal(AllPlugins.Length, sitePlugins.Count);
            Assert.Equal(AllPlugins.Length, sitePlugins.Select(p => p.Id).Distinct().Count());
            Assert.All(sitePlugins, p => Assert.False(manager.IsBuiltin(p)));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
