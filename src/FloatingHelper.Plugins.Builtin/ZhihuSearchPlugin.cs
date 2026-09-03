using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.Builtin;

/// <summary>知乎搜索插件：用默认浏览器在「知乎」中搜索选中文本。</summary>
public sealed class ZhihuSearchPlugin : UrlLaunchPluginBase
{
    public override string Id => "builtin.search.zhihu";

    public override string Name => "知乎";

    public override string Icon => "\uE721";

    public override string Description => "在浏览器中打开知乎搜索选中文本";

    protected override string UrlTemplate => "https://www.zhihu.com/search?type=content&q={0}";
}
