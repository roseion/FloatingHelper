using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.DeepSeekAsk;

/// <summary>
/// DeepSeek 提问插件：打开 DeepSeek 网页对话，并带上选中文本（q 参数）。
/// 说明：若浏览器安装了 DeepSeek 社区增强脚本（如 deepseek-prompt-automation），
/// 页面会读取 q 参数自动填入并发送；未安装时则停留在对话页，粘贴后发送即可。
/// </summary>
public sealed class DeepSeekAskPlugin : IPlugin
{
    private const string UrlTemplate = "https://chat.deepseek.com/a/chat?q={0}";

    public string Id => "site.ask.deepseek";

    public string Name => "DeepSeek";

    public string Icon => "\uE8F1";

    public string Description => "在浏览器中打开 DeepSeek 对话页并带入选中文本作为提问";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>把选中文本拼成 DeepSeek 对话页 URL（q 参数）。</summary>
    public string BuildUrl(string text) => SearchUrlBuilder.BuildSearchUrl(text, UrlTemplate);

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var ok = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
