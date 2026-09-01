using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.Builtin;

/// <summary>搜索插件：用默认浏览器搜索选中文本。</summary>
public sealed class SearchPlugin : IPlugin
{
    public string Id => "builtin.search";

    public string Name => "搜索";

    public string Icon => "\uE721";

    public string Description => "使用默认浏览器搜索选中文本";

    public bool IsEnabled { get; set; } = true;

    /// <summary>可配置的搜索模板，{0} 为编码后的查询词；为 null 时使用默认模板。</summary>
    public string? SearchTemplate { get; set; }

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var url = SearchUrlBuilder.BuildSearchUrl(context.SelectedText, SearchTemplate);
        var ok = ProcessLauncher.Open(url);
        return Task.FromResult<string?>(ok ? null : "搜索失败：无法打开浏览器");
    }
}
