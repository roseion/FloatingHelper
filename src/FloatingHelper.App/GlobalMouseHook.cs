using System.Runtime.InteropServices;

namespace FloatingHelper.App;

/// <summary>
/// 低层全局鼠标钩子（WH_MOUSE_LL）：感知「按下左键并移动后抬起」的拖选动作。
/// </summary>
public sealed class GlobalMouseHook : IDisposable
{
    private const int WhMouseLl = 14;
    private const uint WmLButtonDown = 0x0201;
    private const uint WmLButtonUp = 0x0202;
    private const uint WmMouseMove = 0x0200;

    private readonly LowLevelMouseProc _proc;
    private readonly IntPtr _moduleHandle;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _leftDown;
    private bool _movedWhileDown;

    /// <summary>拖选结束事件（按下左键、产生位移后抬起时触发）。</summary>
    public event Action? SelectionFinished;

    public GlobalMouseHook()
    {
        _proc = HookCallback;
        _moduleHandle = GetModuleHandle(null);
    }

    public void Start()
    {
        _hookId = SetWindowsHookEx(WhMouseLl, _proc, _moduleHandle, 0);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = (uint)wParam;
            if (msg == WmLButtonDown)
            {
                _leftDown = true;
                _movedWhileDown = false;
            }
            else if (msg == WmMouseMove && _leftDown)
            {
                _movedWhileDown = true;
            }
            else if (msg == WmLButtonUp && _leftDown && _movedWhileDown)
            {
                _leftDown = false;
                _movedWhileDown = false;
                SelectionFinished?.Invoke();
            }
            else if (msg == WmLButtonUp)
            {
                _leftDown = false;
                _movedWhileDown = false;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT Pt;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
