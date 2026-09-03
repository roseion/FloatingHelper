using System.Text.Encodings.Web;
using System.Text.Json;
using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.DoubaoAsk;

/// <summary>
/// 豆包提问插件：打开豆包网页版，并通过官方 url-action 协议尝试把选中文本作为提问自动发送；
/// 同时把提问复制到剪贴板兜底。已登录豆包网页版时页面加载后会自动填入并提交，
/// 未登录或协议失效时粘贴（Ctrl+V）并回车即可发送。
/// </summary>
public sealed class DoubaoAskPlugin : IPlugin
{
    // 保留中文等非 ASCII 字符为字面量，生成的 URL 更短、更贴近豆包已验证的协议格式。
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public string Id => "site.ask.doubao";

    public string Name => "豆包";

    public string Icon => "\uE72E";

    public string Description => "打开豆包网页版并尝试自动发送选中文本，同时复制到剪贴板兜底";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    /// <summary>
    /// 构造豆包「发送消息」跳转 URL。
    /// action 参数是 JSON（{"pluginId":"Send_Message","payload":{"text":"..."}}），
    /// 用 JsonSerializer 生成可正确处理引号 / 反斜杠等转义，再做一次 URL 编码。
    /// </summary>
    public string BuildUrl(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("文本不能为空。", nameof(text));
        }

        var json = JsonSerializer.Serialize(new
        {
            pluginId = "Send_Message",
            payload = new { text = text.Trim() },
        }, JsonOptions);

        return "https://www.doubao.com/chat/url-action?action=" + Uri.EscapeDataString(json);
    }

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        // url-action 协议在已登录豆包网页版时可自动填入并发送；
        // 同时把提问复制到剪贴板兜底，即使未登录 / 协议失效，粘贴后回车即可发送。
        var opened = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        var copied = ClipboardHelper.CopyText(context.SelectedText);

        if (!opened)
        {
            return Task.FromResult<string?>("打开失败：无法启动浏览器");
        }

        return Task.FromResult<string?>(copied
            ? "已打开豆包并尝试自动发送，提问已复制到剪贴板（如未自动发送，粘贴后回车即可）"
            : "已打开豆包并尝试自动发送");
    }
}
