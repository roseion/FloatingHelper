using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.DeepSeekAsk;

/// <summary>
/// DeepSeek 提问插件：打开 DeepSeek 网页对话，并把选中文本复制到剪贴板。
/// DeepSeek 网页版原生不支持 URL 参数自动填入 / 自动发送（需浏览器安装社区增强脚本），
/// 因此采用「打开对话页 + 复制提问文本」的可靠方式，打开后粘贴（Ctrl+V）并回车即可发送。
/// </summary>
public sealed class DeepSeekAskPlugin : IPlugin
{
    private const string ChatPageUrl = "https://chat.deepseek.com/a/chat";

    public string Id => "site.ask.deepseek";

    public string Name => "DeepSeek";

    public string Icon => "\uE8F1";

    public string Description => "打开 DeepSeek 对话页，并将选中文本复制到剪贴板（粘贴后发送）";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>DeepSeek 对话页地址（无公开的 URL 预填协议）。</summary>
    public string BuildUrl(string text) => ChatPageUrl;

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var opened = ProcessLauncher.Open(ChatPageUrl);
        var copied = ClipboardHelper.CopyText(context.SelectedText);

        if (!opened)
        {
            return Task.FromResult<string?>("打开失败：无法启动浏览器");
        }

        return Task.FromResult<string?>(copied
            ? "已打开 DeepSeek，提问已复制到剪贴板（粘贴后发送）"
            : "已打开 DeepSeek，但复制剪贴板失败");
    }
}
