using System.Runtime.InteropServices;
using System.Windows;

namespace FloatingHelper.App;

/// <summary>
/// 显示器与 DPI 辅助：将 GetCursorPos 返回的物理像素坐标转换为 WPF DIP，
/// 并获取鼠标所在显示器的工作区（支持多显示器与不同 DPI 缩放）。
/// </summary>
internal static class DisplayHelper
{
    private const int MonitorDefaultToNearest = 2;
    private const int MdtEffectiveDpi = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int Size;
        public RECT Monitor;
        public RECT Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);

    /// <summary>将物理像素坐标转换为 WPF DIP（按鼠标所在显示器的有效 DPI）。</summary>
    public static (double X, double Y) PhysicalToDip(double x, double y)
    {
        var pt = new POINT { X = (int)x, Y = (int)y };
        var hmon = MonitorFromPoint(pt, MonitorDefaultToNearest);
        var scale = GetDpiScale(hmon);
        return (x * scale, y * scale);
    }

    /// <summary>获取指定物理坐标所在显示器的工作区（已转换为 WPF DIP）。</summary>
    public static Rect GetWorkAreaDip(double x, double y)
    {
        var pt = new POINT { X = (int)x, Y = (int)y };
        var hmon = MonitorFromPoint(pt, MonitorDefaultToNearest);
        if (hmon == IntPtr.Zero)
        {
            return SystemParameters.WorkArea;
        }

        var mi = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfo(hmon, ref mi))
        {
            return SystemParameters.WorkArea;
        }

        var scale = GetDpiScale(hmon);
        return new Rect(
            mi.Work.Left * scale,
            mi.Work.Top * scale,
            (mi.Work.Right - mi.Work.Left) * scale,
            (mi.Work.Bottom - mi.Work.Top) * scale);
    }

    private static double GetDpiScale(IntPtr hmon)
    {
        if (hmon == IntPtr.Zero || GetDpiForMonitor(hmon, MdtEffectiveDpi, out var dpiX, out _) != 0)
        {
            return 1.0;
        }

        return dpiX == 0 ? 1.0 : 96.0 / dpiX;
    }
}
