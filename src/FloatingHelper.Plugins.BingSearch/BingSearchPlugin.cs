using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.BingSearch;

/// <summary>必应搜索插件：用默认浏览器在「必应」中搜索选中文本。</summary>
public sealed class BingSearchPlugin : IPlugin
{
    private const string UrlTemplate = "https://www.bing.com/search?q={0}";

    public string Id => "site.search.bing";

    public string Name => "必应";

    public string Icon => "\uE774";

    public string Description => "在浏览器中打开必应搜索选中文本";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>把选中文本拼成必应搜索结果页 URL。</summary>
    public string BuildUrl(string text) => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var ok = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
