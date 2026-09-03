using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.AmapSearch;

/// <summary>高德地图搜索插件：用默认浏览器在「高德地图」中搜索选中文本对应的地点/关键词。</summary>
public sealed class AmapSearchPlugin : IPlugin
{
    private const string UrlTemplate = "https://www.amap.com/search?query={0}";

    public string Id => "site.search.amap";

    public string Name => "高德地图";

    public string Icon => "\uE81D";

    public string Description => "在浏览器中打开高德地图搜索选中文本";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>把选中文本拼成高德地图搜索页 URL。</summary>
    public string BuildUrl(string text) => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var ok = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
