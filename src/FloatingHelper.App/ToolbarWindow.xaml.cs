using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.App;

/// <summary>
/// 无边框、置顶、不抢占焦点的浮层工具栏。按钮由适配当前选区的插件动态生成。
/// </summary>
public partial class ToolbarWindow : Window
{
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;

    private readonly IReadOnlyList<IPlugin> _plugins;
    private readonly PluginContext _context;

    public ToolbarWindow(IReadOnlyList<IPlugin> plugins, PluginContext context)
    {
        InitializeComponent();
        _plugins = plugins;
        _context = context;
        BuildButtons();
        SourceInitialized += (_, _) => ApplyNoActivateStyle();
    }

    private void BuildButtons()
    {
        foreach (var plugin in _plugins)
        {
            var button = new Button
            {
                Content = plugin.Name,
                Tag = plugin,
                Margin = new Thickness(4, 0, 4, 0),
                Padding = new Thickness(12, 6, 12, 6),
                FontSize = 13,
                Cursor = System.Windows.Input.Cursors.Hand,
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

        // 插件异常隔离：单个插件失败不影响主程序与其他插件。
        try
        {
            await plugin.ExecuteAsync(_context);
        }
        catch
        {
            // 忽略插件异常。
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
