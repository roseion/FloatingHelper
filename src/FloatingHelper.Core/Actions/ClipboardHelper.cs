using System.Runtime.InteropServices;
using System.Text;

namespace FloatingHelper.Core.Actions;

/// <summary>
/// 基于 Win32 的剪贴板写入，不依赖 WPF 线程模型（可在非 STA 线程工作）。
/// </summary>
public static class ClipboardHelper
{
    private const uint CfUnicodeText = 13;
    private const int MaxRetries = 5;

    /// <summary>将文本写入系统剪贴板。剪贴板被占用时会短暂重试。</summary>
    public static bool CopyText(string? text)
    {
        if (text is null)
        {
            return false;
        }

        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            if (TryCopyText(text))
            {
                return true;
            }
            Thread.Sleep(50);
        }

        return false;
    }

    private static bool TryCopyText(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            return false;
        }

        try
        {
            if (!EmptyClipboard())
            {
                return false;
            }

            var bytes = Encoding.Unicode.GetBytes(text + "\0");
            var hGlobal = Marshal.AllocHGlobal(bytes.Length);
            var ownershipTransferred = false;
            try
            {
                Marshal.Copy(bytes, 0, hGlobal, bytes.Length);
                ownershipTransferred = SetClipboardData(CfUnicodeText, hGlobal) != IntPtr.Zero;
                return ownershipTransferred;
            }
            finally
            {
                // 只有所有权未转移（SetClipboardData 失败）时才需要释放内存。
                if (!ownershipTransferred)
                {
                    Marshal.FreeHGlobal(hGlobal);
                }
            }
        }
        finally
        {
            CloseClipboard();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();
}
