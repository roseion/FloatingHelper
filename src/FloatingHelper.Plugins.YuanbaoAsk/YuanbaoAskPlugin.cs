using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.YuanbaoAsk;

/// <summary>
/// 腾讯元宝提问插件：打开元宝网页对话页，并把选中文本复制到剪贴板。
/// 元宝网页版暂未提供公开的 URL 预填 / 自动发送协议，因此采用「打开对话页 + 复制提问文本」，
/// 打开后粘贴（Ctrl+V）并回车即可发送。
/// </summary>
public sealed class YuanbaoAskPlugin : IPlugin
{
    private const string ChatPageUrl = "https://yuanbao.tencent.com/chat/";

    public string Id => "site.ask.yuanbao";

    public string Name => "元宝";

    public string Icon => "\uE8BD";

    public string Description => "打开腾讯元宝对话页，并将选中文本复制到剪贴板（粘贴即可发送）";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>元宝对话页地址（无公开的 URL 预填协议）。</summary>
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
            ? "已打开元宝，提问已复制到剪贴板（粘贴后发送）"
            : "已打开元宝，但复制剪贴板失败");
    }
}
