using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.BaiduMapSearch;

/// <summary>
/// 百度地图搜索插件：用默认浏览器打开「百度地图」，并尽量预填搜索框搜索选中文本。
/// 百度地图网页版（2026）不支持 URL 参数直达搜索：仅带 <c>/search/{kw}</c> 打开时搜索框为空、地图停在当前位置。
/// 实测带 <c>?querytype=s&amp;wd={kw}</c> 时多数情况下可把关键词填入搜索框并定位到对应城市。
/// 为保证一定可用，同时把关键词复制到剪贴板：若未自动填入，粘贴（Ctrl+V）到搜索框回车即可搜索。
/// </summary>
public sealed class BaiduMapSearchPlugin : IPlugin
{
    private const string UrlTemplate = "https://map.baidu.com/search/{0}?querytype=s&wd={0}";

    public string Id => "site.search.baidumap";

    public string Name => "百度地图";

    public string Icon => "\uE707";

    public string Description => "打开百度地图搜索选中文本，并复制关键词到剪贴板兜底";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>把选中文本拼成百度地图搜索页 URL（路径式 + 预填参数，{0} 为 URL 编码后的关键词）。</summary>
    public string BuildUrl(string text) => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        // 百度地图网页版不支持 URL 自动执行搜索，只保证「打开 + 尽量预填」；
        // 复制关键词到剪贴板兜底，未自动填入时粘贴到搜索框回车即可搜索。
        var opened = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        var copied = ClipboardHelper.CopyText(context.SelectedText);

        if (!opened)
        {
            return Task.FromResult<string?>("打开失败：无法启动浏览器");
        }

        return Task.FromResult<string?>(copied
            ? "已打开百度地图，关键词已复制到剪贴板（如未自动填入，粘贴后回车即可搜索）"
            : "已打开百度地图，但复制剪贴板失败");
    }
}
