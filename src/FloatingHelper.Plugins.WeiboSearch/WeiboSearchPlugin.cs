using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.WeiboSearch;

/// <summary>微博搜索插件：用默认浏览器在「微博」中搜索选中文本。</summary>
public sealed class WeiboSearchPlugin : IPlugin
{
    private const string UrlTemplate = "https://s.weibo.com/weibo?q={0}";

    public string Id => "site.search.weibo";

    public string Name => "微博";

    public string Icon => "\uE897";

    public string Description => "在浏览器中打开微博搜索选中文本";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>把选中文本拼成微博搜索结果页 URL。</summary>
    public string BuildUrl(string text) => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var ok = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
