using System.Windows;

namespace FloatingHelper.Core.Selection;

/// <summary>一次成功的选区捕获结果。Bounds 为物理像素坐标，App 层负责转换为 DIP。</summary>
public sealed record TextSelection(string Text, string? ProcessName, Rect? Bounds = null);
