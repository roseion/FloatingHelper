using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.Builtin;

/// <summary>
/// 智能打开插件：识别选中文本类型（URL / 本地路径 / 邮箱），按对应方式打开。
/// </summary>
public sealed class SmartOpenPlugin : IPlugin
{
    public string Id => "builtin.smartopen";

    public string Name => "打开";

    public string Icon => "\uE8A7";

    public string Description => "识别选中文本并按对应方式打开（链接 / 文件路径 / 邮箱）";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context)
    {
        if (!context.HasMeaningfulText)
        {
            return false;
        }

        // 仅当能识别为 URL / 存在的本地路径 / 邮箱时才展示「打开」动作。
        return SmartOpenTypeDetector.Detect(context.SelectedText) != OpenTargetType.PlainText;
    }

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var text = context.SelectedText.Trim();
        var target = ResolveTarget(text);
        if (target is null)
        {
            return Task.FromResult<string?>(null);
        }

        var ok = ProcessLauncher.Open(target);
        return Task.FromResult<string?>(ok ? null : "打开失败：未找到关联程序");
    }

    private static string? ResolveTarget(string text)
    {
        return SmartOpenTypeDetector.Detect(text) switch
        {
            OpenTargetType.Url or OpenTargetType.FilePath => text.Trim('"'),
            OpenTargetType.Email => "mailto:" + text,
            _ => null,
        };
    }
}
