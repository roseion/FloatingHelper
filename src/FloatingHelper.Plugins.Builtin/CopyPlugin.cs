using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.Plugins.Builtin;

/// <summary>复制插件：将选中文本写入系统剪贴板。</summary>
public sealed class CopyPlugin : IPlugin
{
    public string Id => "builtin.copy";

    public string Name => "复制";

    public string Description => "复制选中文本到系统剪贴板";

    public bool IsEnabled { get; set; } = true;

    public bool CanHandle(PluginContext context) => context.HasMeaningfulText;

    public Task<string?> ExecuteAsync(PluginContext context, CancellationToken cancellationToken = default)
    {
        var ok = ClipboardHelper.CopyText(context.SelectedText);
        return Task.FromResult<string?>(ok ? $"已复制 {context.SelectedText.Length} 个字符" : "复制失败：剪贴板被占用");
    }
}
