using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.XiaohongshuSearch;

/// <summary>小红书搜索插件：用默认浏览器在「小红书」中搜索选中文本。</summary>
public sealed class XiaohongshuSearchPlugin : IPlugin
{
    private const string UrlTemplate = "https://www.xiaohongshu.com/search_result?keyword={0}&source=web_explore_feed";

    public string Id => "site.search.xiaohongshu";

    public string Name => "小红书";

    public string Icon => "\uE774";

    public string Description => "在浏览器中打开小红书搜索选中文本";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>把选中文本拼成小红书搜索结果页 URL。</summary>
    public string BuildUrl(string text) => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var ok = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
