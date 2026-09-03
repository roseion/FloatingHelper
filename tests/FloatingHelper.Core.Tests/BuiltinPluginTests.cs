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

    [Theory]
    [InlineData("小红书文案")]
    [InlineData("https://example.com")]
    public void XiaohongshuSearchPlugin_CanHandle_MeaningfulText_ShouldBeTrue(string text)
    {
        Assert.True(new XiaohongshuSearchPlugin().CanHandle(Ctx(text)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void XiaohongshuSearchPlugin_CanHandle_Blank_ShouldBeFalse(string text)
    {
        Assert.False(new XiaohongshuSearchPlugin().CanHandle(Ctx(text)));
    }

    [Fact]
    public void XiaohongshuSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new XiaohongshuSearchPlugin().BuildUrl("测试 小红书");
        Assert.StartsWith("https://www.xiaohongshu.com/search_result?keyword=", url);
        Assert.Contains("source=web_explore_feed", url);
        Assert.DoesNotContain(" ", url);
    }

    [Theory]
    [InlineData("某种技术")]
    public void ZhihuSearchPlugin_CanHandle_MeaningfulText_ShouldBeTrue(string text)
    {
        Assert.True(new ZhihuSearchPlugin().CanHandle(Ctx(text)));
    }

    [Fact]
    public void ZhihuSearchPlugin_CanHandle_Blank_ShouldBeFalse()
    {
        Assert.False(new ZhihuSearchPlugin().CanHandle(Ctx(" ")));
    }

    [Fact]
    public void ZhihuSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new ZhihuSearchPlugin().BuildUrl("hello world");
        Assert.Equal("https://www.zhihu.com/search?type=content&q=hello%20world", url);
    }

    [Fact]
    public void WeiboSearchPlugin_CanHandle_MeaningfulText_ShouldBeTrue()
    {
        Assert.True(new WeiboSearchPlugin().CanHandle(Ctx("热搜话题")));
    }

    [Fact]
    public void WeiboSearchPlugin_CanHandle_Blank_ShouldBeFalse()
    {
        Assert.False(new WeiboSearchPlugin().CanHandle(Ctx("  ")));
    }

    [Fact]
    public void WeiboSearchPlugin_BuildUrl_ShouldEncodeKeyword()
    {
        var url = new WeiboSearchPlugin().BuildUrl("热点 事件");
        Assert.StartsWith("https://s.weibo.com/weibo?q=", url);
        Assert.DoesNotContain(" ", url);
    }

    [Fact]
    public void DoubaoAskPlugin_CanHandle_MeaningfulText_ShouldBeTrue()
    {
        Assert.True(new DoubaoAskPlugin().CanHandle(Ctx("帮我写段文案")));
    }

    [Fact]
    public void DoubaoAskPlugin_CanHandle_Blank_ShouldBeFalse()
    {
        Assert.False(new DoubaoAskPlugin().CanHandle(Ctx("   ")));
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
    public void DeepSeekAskPlugin_CanHandle_MeaningfulText_ShouldBeTrue()
    {
        Assert.True(new DeepSeekAskPlugin().CanHandle(Ctx("解释一下 RAG")));
    }

    [Fact]
    public void DeepSeekAskPlugin_CanHandle_Blank_ShouldBeFalse()
    {
        Assert.False(new DeepSeekAskPlugin().CanHandle(Ctx("")));
    }

    [Fact]
    public void DeepSeekAskPlugin_BuildUrl_ShouldEncodeQuestion()
    {
        var url = new DeepSeekAskPlugin().BuildUrl("什么是 YOLO");
        Assert.Equal("https://chat.deepseek.com/a/chat?q=%E4%BB%80%E4%B9%88%E6%98%AF%20YOLO", url);
    }

    [Fact]
    public void YuanbaoAskPlugin_CanHandle_MeaningfulText_ShouldBeTrue()
    {
        Assert.True(new YuanbaoAskPlugin().CanHandle(Ctx("帮我总结")));
    }

    [Fact]
    public void YuanbaoAskPlugin_CanHandle_Blank_ShouldBeFalse()
    {
        Assert.False(new YuanbaoAskPlugin().CanHandle(Ctx(" ")));
    }

    [Fact]
    public void YuanbaoAskPlugin_BuildUrl_ShouldPointToChatPage()
    {
        var url = new YuanbaoAskPlugin().BuildUrl("任意文本");
        Assert.Equal("https://yuanbao.tencent.com/chat/", url);
    }

    [Fact]
    public void BuiltinPlugins_ShouldHaveUniqueIds()
    {
        IPlugin[] plugins =
        [
            new CopyPlugin(),
            new SmartOpenPlugin(),
            new SearchPlugin(),
            new XiaohongshuSearchPlugin(),
            new ZhihuSearchPlugin(),
            new WeiboSearchPlugin(),
            new DoubaoAskPlugin(),
            new YuanbaoAskPlugin(),
            new DeepSeekAskPlugin(),
        ];
        var ids = plugins.Select(p => p.Id).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }
}
