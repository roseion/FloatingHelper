using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.GoogleSearch;

/// <summary>谷歌搜索插件：用默认浏览器在「谷歌」中搜索选中文本。</summary>
public sealed class GoogleSearchPlugin : IPlugin
{
    private const string UrlTemplate = "https://www.google.com/search?q={0}";

    public string Id => "site.search.google";

    public string Name => "谷歌";

    public string Icon => "\uE72A";

    public string Description => "在浏览器中打开谷歌搜索选中文本";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>把选中文本拼成谷歌搜索结果页 URL。</summary>
    public string BuildUrl(string text) => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var ok = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
