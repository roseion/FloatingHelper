using System.Text.Encodings.Web;
using System.Text.Json;
using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.DoubaoAsk;

/// <summary>
/// 豆包提问插件：打开豆包网页版，并通过官方 url-action 协议把选中文本作为提问自动发送。
/// 页面加载完成后会自动在对话输入框填入并提交，无需再手动粘贴。
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

    public string Description => "打开豆包网页版并自动发送选中文本作为提问";

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
        var ok = ProcessLauncher.Open(BuildUrl(context.SelectedText));
        return Task.FromResult<string?>(ok ? null : "打开失败：无法启动浏览器");
    }
}
