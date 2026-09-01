using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.App;

/// <summary>
/// 无边框、置顶、不抢占焦点的浮层工具栏。按钮由适配当前选区的插件动态生成，带图标与悬停反馈。
/// 无操作 5 秒自动消失，点击工具栏外区域由调用方关闭。
/// </summary>
public partial class ToolbarWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private static readonly TimeSpan AutoCloseDelay = TimeSpan.FromSeconds(5);

    private readonly IReadOnlyList<IPlugin> _plugins;
    private readonly PluginContext _context;
    private readonly DispatcherTimer _autoCloseTimer;

    /// <summary>插件执行后返回文本结果时触发（参数：结果文本 + 执行上下文）。</summary>
    public event Action<string, PluginContext>? PluginResultReady;

    public ToolbarWindow(IReadOnlyList<IPlugin> plugins, PluginContext context)
    {
        InitializeComponent();
        _plugins = plugins;
        _context = context;
        BuildButtons();
        SourceInitialized += (_, _) => ApplyNoActivateStyle();

        _autoCloseTimer = new DispatcherTimer { Interval = AutoCloseDelay };
        _autoCloseTimer.Tick += (_, _) => Close();
        _autoCloseTimer.Start();

        // 鼠标悬停在工具栏上时暂停自动关闭，离开后恢复。
        MouseEnter += (_, _) => _autoCloseTimer.Stop();
        MouseLeave += (_, _) => _autoCloseTimer.Start();
    }

    /// <summary>判断指定 DIP 坐标点是否落在工具栏窗口范围内。</summary>
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

    private void BuildButtons()
    {
        foreach (var plugin in _plugins)
        {
            var content = new StackPanel { Orientation = Orientation.Horizontal };

            var icon = new TextBlock
            {
                Text = plugin.Icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            };

            var label = new TextBlock
            {
                Text = plugin.Name,
                VerticalAlignment = VerticalAlignment.Center,
            };

            content.Children.Add(icon);
            content.Children.Add(label);

            var button = new Button
            {
                Content = content,
                Tag = plugin,
                Style = (Style)FindResource("ToolbarButton"),
            };
            button.Click += OnButtonClick;
            ButtonPanel.Children.Add(button);
        }
    }

    private async void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is not IPlugin plugin)
        {
            return;
        }

        var context = _context;
        Close(); // 先关闭工具栏，再执行插件动作

        string? result = null;
        try
        {
            result = await plugin.ExecuteAsync(context);
        }
        catch
        {
            // 插件异常隔离：单个插件失败不影响主程序。
        }

        if (!string.IsNullOrEmpty(result))
        {
            PluginResultReady?.Invoke(result, context);
        }
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
