using System.Runtime.InteropServices;
using FloatingHelper.Core.Logging;

namespace FloatingHelper.Core.Selection;

/// <summary>
/// 剪贴板降级捕获：当目标应用不暴露 UIA TextPattern（如微信、WorkBuddy 等封闭应用）时，
/// 通过模拟 Ctrl+C 将选区复制到系统剪贴板，再读取文本。
/// 读取后自动恢复原剪贴板内容，避免覆盖用户剪贴板。
/// 全部基于 Win32 API，可在任意线程使用（不依赖 STA / WPF）。
/// </summary>
public static class ClipboardCaptureService
{
    private const byte VkControl = 0x11;
    private const byte VkC = 0x43;
    private const uint KeyEventfKeyUp = 0x0002;
    private const uint CfUnicodeText = 13;

    // 防止异常大的剪贴板格式拖垮进程（如超大位图）。
    private const int MaxFormatBytes = 64 * 1024 * 1024;

    private static readonly TimeSpan MaxWait = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(30);

    /// <summary>
    /// 模拟 Ctrl+C 并读取剪贴板中的选中文本；读取后无论成功与否都恢复原剪贴板。
    /// 未检测到剪贴板变化或文本为空时返回 null。
    /// </summary>
    public static string? TryCaptureSelectedText()
    {
        var snapshot = CaptureSnapshot();
        try
        {
            var beforeSeq = GetClipboardSequenceNumber();
            SendCtrlC();

            var text = WaitForClipboardText(beforeSeq);
            if (text is not null)
            {
                Logger.Info($"[剪贴板降级捕获] 成功，长度={text.Length}");
            }
            return text;
        }
        finally
        {
            RestoreSnapshot(snapshot);
        }
    }

    /// <summary>快照当前剪贴板中所有可复制格式（格式 → 原始字节）。</summary>
    public static ClipboardSnapshot CaptureSnapshot()
    {
        var formats = new Dictionary<uint, byte[]>();
        if (!OpenClipboardWithRetry())
        {
            return new ClipboardSnapshot(formats);
        }

        try
        {
            var format = 0u;
            while ((format = EnumClipboardFormats(format)) != 0)
            {
                var hData = GetClipboardData(format);
                if (hData == IntPtr.Zero)
                {
                    continue;
                }

                var size = GlobalSize(hData);
                if (size <= 0 || size > MaxFormatBytes)
                {
                    continue;
                }

                var ptr = GlobalLock(hData);
                if (ptr == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    var bytes = new byte[(int)size];
                    Marshal.Copy(ptr, bytes, 0, bytes.Length);
                    formats[format] = bytes;
                }
                finally
                {
                    GlobalUnlock(hData);
                }
            }
        }
        finally
        {
            CloseClipboard();
        }

        return new ClipboardSnapshot(formats);
    }

    /// <summary>将剪贴板恢复到指定快照；快照为空则清空剪贴板。</summary>
    public static void RestoreSnapshot(ClipboardSnapshot snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        if (!OpenClipboardWithRetry())
        {
            Logger.Warn("[剪贴板降级捕获] 恢复失败：无法打开剪贴板");
            return;
        }

        try
        {
            if (!EmptyClipboard())
            {
                Logger.Warn("[剪贴板降级捕获] 恢复失败：清空剪贴板失败");
                return;
            }

            foreach (var (format, bytes) in snapshot.Formats)
            {
                var hGlobal = Marshal.AllocHGlobal(bytes.Length);
                var transferred = false;
                try
                {
                    Marshal.Copy(bytes, 0, hGlobal, bytes.Length);
                    transferred = SetClipboardData(format, hGlobal) != IntPtr.Zero;
                }
                catch
                {
                    // 单格式写入失败不影响其它格式。
                }
                finally
                {
                    // 只有所有权未转移（SetClipboardData 失败）时才释放内存。
                    if (!transferred)
                    {
                        Marshal.FreeHGlobal(hGlobal);
                    }
                }
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    /// <summary>读取剪贴板 Unicode 文本，为空返回 null。</summary>
    public static string? ReadClipboardText()
    {
        if (!OpenClipboardWithRetry())
        {
            return null;
        }

        try
        {
            var hData = GetClipboardData(CfUnicodeText);
            if (hData == IntPtr.Zero)
            {
                return null;
            }

            var ptr = GlobalLock(hData);
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var text = Marshal.PtrToStringUni(ptr);
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }
            finally
            {
                GlobalUnlock(hData);
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    private static string? WaitForClipboardText(uint beforeSeq)
    {
        var deadline = Environment.TickCount64 + (long)MaxWait.TotalMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            if (GetClipboardSequenceNumber() != beforeSeq)
            {
                var text = ReadClipboardText();
                if (text is not null)
                {
                    return text;
                }
            }
            Thread.Sleep(PollInterval);
        }

        return null;
    }

    private static bool OpenClipboardWithRetry()
    {
        for (var i = 0; i < 5; i++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                return true;
            }
            Thread.Sleep(40);
        }

        return false;
    }

    /// <summary>通过 SendInput 模拟 Ctrl+C 按键，发给当前前台窗口。</summary>
    private static void SendCtrlC()
    {
        var inputs = new[]
        {
            new INPUT { Type = InputKeyboard, Data = new InputUnion { Ki = new KEYBDINPUT { WVk = VkControl } } },
            new INPUT { Type = InputKeyboard, Data = new InputUnion { Ki = new KEYBDINPUT { WVk = VkC } } },
            new INPUT { Type = InputKeyboard, Data = new InputUnion { Ki = new KEYBDINPUT { WVk = VkC, DwFlags = KeyEventfKeyUp } } },
            new INPUT { Type = InputKeyboard, Data = new InputUnion { Ki = new KEYBDINPUT { WVk = VkControl, DwFlags = KeyEventfKeyUp } } },
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private const uint InputKeyboard = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT Ki;
        [FieldOffset(0)] public MOUSEINPUT Mi;
        [FieldOffset(0)] public HARDWAREINPUT Hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint UMsg;
        public ushort WLParamL;
        public ushort WLParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern uint EnumClipboardFormats(uint format);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, IntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalSize(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}

/// <summary>剪贴板快照：格式 → 原始字节。</summary>
public sealed record ClipboardSnapshot(IReadOnlyDictionary<uint, byte[]> Formats);
