using System.Diagnostics;

namespace FloatingHelper.Core.Actions;

/// <summary>
/// 按系统默认关联程序打开文件 / 目录 / URL（通过 ShellExecute）。
/// </summary>
public static class ProcessLauncher
{
    /// <summary>
    /// 使用系统默认程序打开目标（文件、目录或 URL）。
    /// </summary>
    /// <returns>启动是否成功。</returns>
    public static bool Open(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = target.Trim(),
                UseShellExecute = true,
            };
            return Process.Start(startInfo) is not null;
        }
        catch
        {
            return false;
        }
    }
}
