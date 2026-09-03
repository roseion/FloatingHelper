using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.SiteLauncher;

/// <summary>小红书搜索插件：用默认浏览器在「小红书」中搜索选中文本。</summary>
public sealed class XiaohongshuSearchPlugin : UrlLaunchPluginBase
{
    public override string Id => "site.search.xiaohongshu";

    public override string Name => "小红书";

    public override string Icon => "\uE774";

    public override string Description => "在浏览器中打开小红书搜索选中文本";

    protected override string UrlTemplate => "https://www.xiaohongshu.com/search_result?keyword={0}&source=web_explore_feed";
}
