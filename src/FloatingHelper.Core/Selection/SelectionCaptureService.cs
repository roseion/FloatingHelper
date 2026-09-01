using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace FloatingHelper.Core.Selection;

/// <summary>
/// 通过 UI Automation 的 TextPattern 读取前台聚焦元素的当前选区文本与位置。
/// 读取失败时返回 null（静默降级）。
/// </summary>
public static class SelectionCaptureService
{
    /// <summary>
    /// 尝试捕获当前聚焦元素中的选中文本及屏幕边界。
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

            var bounds = TryGetSelectionBounds(ranges[0]);
            return new TextSelection(text, GetProcessName(textElement), bounds);
        }
        catch
        {
            // 目标应用不支持标准选区读取时静默降级。
            return null;
        }
    }

    /// <summary>
    /// 从 TextPatternRange 获取选区的联合边界矩形（物理像素）。
    /// 多行选区可能返回多个矩形，取外包矩形。获取失败返回 null。
    /// </summary>
    private static Rect? TryGetSelectionBounds(TextPatternRange range)
    {
        try
        {
            var rects = range.GetBoundingRectangles();
            if (rects is null || rects.Length == 0)
            {
                return null;
            }

            double left = double.MaxValue, top = double.MaxValue;
            double right = double.MinValue, bottom = double.MinValue;
            var hasValid = false;

            foreach (var rect in rects)
            {
                if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0)
                {
                    continue;
                }

                hasValid = true;
                left = Math.Min(left, rect.Left);
                top = Math.Min(top, rect.Top);
                right = Math.Max(right, rect.Right);
                bottom = Math.Max(bottom, rect.Bottom);
            }

            return hasValid ? new Rect(left, top, right - left, bottom - top) : null;
        }
        catch
        {
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
