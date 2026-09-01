using System.Net.Http;
using System.Text;
using System.Text.Json;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.Builtin;

/// <summary>
/// 翻译插件：将选中文字翻译为中文，结果通过 ExecuteAsync 返回值交给主程序浮层显示。
/// 使用谷歌翻译免费接口（无需 API Key），5 秒超时，失败时静默返回 null。
/// </summary>
public sealed class TranslatePlugin : IPlugin
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(5),
    };

    /// <summary>目标语言，默认简体中文。</summary>
    public string TargetLanguage { get; set; } = "zh-CN";

    public string Id => "builtin.translate";
    public string Name => "翻译";
    public string Description => "将选中的文字翻译为中文（谷歌翻译免费接口），结果在选区附近显示。";
    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    public async Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var text = context.SelectedText.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        try
        {
            var url = $"https://translate.googleapis.com/translate_a/single"
                      + $"?client=gtx&sl=auto&tl={Uri.EscapeDataString(TargetLanguage)}&dt=t"
                      + $"&q={Uri.EscapeDataString(text)}";

            using var response = await Http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            return ParseTranslation(json);
        }
        catch
        {
            // 网络异常、超时、解析失败均静默返回 null。
            return null;
        }
    }

    /// <summary>
    /// 解析谷歌翻译返回的 JSON：
    /// [[["译文1","原文1",null,null,10],["译文2",...]],null,"auto",...]
    /// </summary>
    private static string? ParseTranslation(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var sentences = doc.RootElement[0];
            if (sentences.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var sb = new StringBuilder();
            foreach (var sentence in sentences.EnumerateArray())
            {
                if (sentence.ValueKind == JsonValueKind.Array
                    && sentence.GetArrayLength() > 0
                    && sentence[0].ValueKind == JsonValueKind.String)
                {
                    sb.Append(sentence[0].GetString());
                }
            }

            var result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }
}
