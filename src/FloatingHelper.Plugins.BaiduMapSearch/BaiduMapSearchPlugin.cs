using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.BaiduMapSearch;

/// <summary>百度地图搜索插件：用默认浏览器在「百度地图」中搜索选中文本对应的地点/关键词。</summary>
public sealed class BaiduMapSearchPlugin : IPlugin
{
    private const string UrlTemplate = "https://map.baidu.com/search/{0}";

    public string Id => "site.search.baidumap";

    public string Name => "百度地图";

    public string Icon => "\uE707";

    public string Description => "在浏览器中打开百度地图搜索选中文本";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>把选中文本拼成百度地图搜索页 URL（路径式，{0} 为 URL 编码后的关键词）。</summary>
    public string BuildUrl(string text) => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var ok = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
