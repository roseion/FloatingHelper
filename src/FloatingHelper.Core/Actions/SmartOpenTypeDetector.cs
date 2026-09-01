using System.IO;
using System.Text.RegularExpressions;

namespace FloatingHelper.Core.Actions;

/// <summary>智能打开的目标类型。</summary>
public enum OpenTargetType
{
    /// <summary>URL 链接。</summary>
    Url,

    /// <summary>本地文件 / 目录路径。</summary>
    FilePath,

    /// <summary>邮箱地址。</summary>
    Email,

    /// <summary>普通文本（兜底）。</summary>
    PlainText,
}

/// <summary>
/// 根据选中文本判断其类型，供「智能打开」插件分发到对应打开方式。
/// </summary>
public static partial class SmartOpenTypeDetector
{
    private const string UrlPattern = @"^(https?://|www\.)[^\s]+$";
    private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    private const string WindowsDrivePathPattern = @"^(?<drive>[A-Za-z]:[\\/]).+$";
    private const string UncPathPattern = @"^\\\\[^\\]+\\";
    private const string DotPathPattern = @"^[.\/]{1,2}[\\/].+$";

    /// <summary>
    /// 判断文本类型。识别优先级：URL → 邮箱 → 路径（需真实存在）→ 普通文本。
    /// 注意：路径类型需要额外做存在性校验（见 <see cref="IsExistingLocalPath"/>）。
    /// </summary>
    public static OpenTargetType Detect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return OpenTargetType.PlainText;
        }

        var trimmed = text.Trim();

        if (IsUrl(trimmed))
        {
            return OpenTargetType.Url;
        }

        if (IsEmail(trimmed))
        {
            return OpenTargetType.Email;
        }

        if (IsLikelyPath(trimmed) && IsExistingLocalPath(trimmed))
        {
            return OpenTargetType.FilePath;
        }

        return OpenTargetType.PlainText;
    }

    /// <summary>是否为 URL 形态。</summary>
    public static bool IsUrl(string text) => UrlRegex().IsMatch(text);

    /// <summary>是否为邮箱形态。</summary>
    public static bool IsEmail(string text) => EmailRegex().IsMatch(text);

    /// <summary>是否为疑似本地路径形态（盘符 / UNC / 相对点路径），与是否存在无关。</summary>
    public static bool IsLikelyPath(string text) =>
        WindowsDrivePathRegex().IsMatch(text) ||
        UncPathRegex().IsMatch(text) ||
        DotPathRegex().IsMatch(text);

    /// <summary>路径存在性校验：文件或目录存在其一即为真。</summary>
    public static bool IsExistingLocalPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
            return File.Exists(expanded) || Directory.Exists(expanded);
        }
        catch
        {
            return false;
        }
    }

    [GeneratedRegex(UrlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(EmailPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(WindowsDrivePathPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex WindowsDrivePathRegex();

    [GeneratedRegex(UncPathPattern, RegexOptions.Compiled)]
    private static partial Regex UncPathRegex();

    [GeneratedRegex(DotPathPattern, RegexOptions.Compiled)]
    private static partial Regex DotPathRegex();
}
