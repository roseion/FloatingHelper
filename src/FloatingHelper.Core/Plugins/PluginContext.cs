using System.Windows;

namespace FloatingHelper.Core.Plugins;

/// <summary>
/// 传给插件执行时的上下文信息：当前选中的文本、来源进程、选区屏幕位置等。
/// </summary>
public sealed class PluginContext
{
    /// <summary>当前用户选中的文本。</summary>
    public required string SelectedText { get; init; }

    /// <summary>产生选区的来源进程名（无扩展名），可能为 null。</summary>
    public string? SourceProcessName { get; init; }

    /// <summary>
    /// 选区在屏幕上的边界矩形（WPF DIP 坐标）。
    /// UIA 无法获取时为 null，此时主程序回退到鼠标位置。
    /// </summary>
    public Rect? SelectionBounds { get; init; }

    /// <summary>选区是否为空或仅空白字符。</summary>
    public bool HasMeaningfulText => !string.IsNullOrWhiteSpace(SelectedText);
}
