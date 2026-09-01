using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using FloatingHelper.Core.Actions;

namespace FloatingHelper.App;

/// <summary>
/// 插件执行结果浮层：在选区附近显示翻译等文本结果，5 秒自动消失，支持一键复制。
/// </summary>
public partial class ResultPopupWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private static readonly TimeSpan AutoCloseDelay = TimeSpan.FromSeconds(5);

    private readonly DispatcherTimer _autoCloseTimer;
    private readonly string _resultText;

    public ResultPopupWindow(string text)
    {
        InitializeComponent();
        _resultText = text;
        ResultText.Text = text;

        SourceInitialized += (_, _) => ApplyNoActivateStyle();

        _autoCloseTimer = new DispatcherTimer { Interval = AutoCloseDelay };
        _autoCloseTimer.Tick += (_, _) => Close();
        _autoCloseTimer.Start();

        MouseEnter += (_, _) => _autoCloseTimer.Stop();
        MouseLeave += (_, _) => _autoCloseTimer.Start();
    }

    /// <summary>判断指定 DIP 坐标点是否落在浮层范围内。</summary>
    public bool IsPointOver(double x, double y)
    {
        return x >= Left && x <= Left + ActualWidth
            && y >= Top && y <= Top + ActualHeight;
    }

    protected override void OnClosed(EventArgs e)
    {
        _autoCloseTimer.Stop();
        base.OnClosed(e);
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        try
        {
            ClipboardHelper.CopyText(_resultText);
        }
        catch
        {
            // 剪贴板被占用时忽略。
        }

        Close();
    }

    private void ApplyNoActivateStyle()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GwlExStyle);
        style |= WsExNoActivate | WsExToolWindow;
        SetWindowLong(hwnd, GwlExStyle, style);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
