using System.Diagnostics;
using System.Windows.Automation;

namespace FloatingHelper.Core.Selection;

/// <summary>
/// 通过 UI Automation 的 TextPattern 读取前台聚焦元素的当前选区文本。
/// 读取失败时返回 null（静默降级）。
/// </summary>
public static class SelectionCaptureService
{
    /// <summary>
    /// 尝试捕获当前聚焦元素中的选中文本。
    /// </summary>
    public static TextSelection? TryCapture()
    {
        try
        {
            var focused = AutomationElement.FocusedElement;
            if (focused is null)
            {
                return null;
            }

            var textElement = FindTextElement(focused);
            if (textElement is null)
            {
                return null;
            }

            var pattern = (TextPattern)textElement.GetCurrentPattern(TextPattern.Pattern);
            var ranges = pattern.GetSelection();
            if (ranges is null || ranges.Length == 0)
            {
                return null;
            }

            var text = ranges[0].GetText(-1);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return new TextSelection(text, GetProcessName(textElement));
        }
        catch
        {
            // 目标应用不支持标准选区读取时静默降级。
            return null;
        }
    }

    /// <summary>优先取自身；自身不支持 TextPattern 时向后代查找第一个支持的元素。</summary>
    private static AutomationElement? FindTextElement(AutomationElement root)
    {
        if (root.TryGetCurrentPattern(TextPattern.Pattern, out _))
        {
            return root;
        }

        var condition = new PropertyCondition(AutomationElement.IsTextPatternAvailableProperty, true);
        return root.FindFirst(TreeScope.Descendants, condition);
    }

    private static string? GetProcessName(AutomationElement element)
    {
        try
        {
            var pid = element.Current.ProcessId;
            using var process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }
}
