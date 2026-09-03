using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.Builtin;

/// <summary>微博搜索插件：用默认浏览器在「微博」中搜索选中文本。</summary>
public sealed class WeiboSearchPlugin : UrlLaunchPluginBase
{
    public override string Id => "builtin.search.weibo";

    public override string Name => "微博";

    public override string Icon => "\uE897";

    public override string Description => "在浏览器中打开微博搜索选中文本";

    protected override string UrlTemplate => "https://s.weibo.com/weibo?q={0}";
}
