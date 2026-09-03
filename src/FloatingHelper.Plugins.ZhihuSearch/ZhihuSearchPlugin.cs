using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.ZhihuSearch;

/// <summary>知乎搜索插件：用默认浏览器在「知乎」中搜索选中文本。</summary>
public sealed class ZhihuSearchPlugin : IPlugin
{
    private const string UrlTemplate = "https://www.zhihu.com/search?type=content&q={0}";

    public string Id => "site.search.zhihu";

    public string Name => "知乎";

    public string Icon => "\uE721";

    public string Description => "在浏览器中打开知乎搜索选中文本";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>把选中文本拼成知乎搜索结果页 URL。</summary>
    public string BuildUrl(string text) => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var ok = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
