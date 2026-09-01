namespace FloatingHelper.Core.Selection;

/// <summary>一次成功的选区捕获结果。</summary>
public sealed record TextSelection(string Text, string? ProcessName);
