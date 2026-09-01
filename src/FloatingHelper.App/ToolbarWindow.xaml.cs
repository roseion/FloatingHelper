using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.App;

/// <summary>
/// 无边框、置顶、不抢占焦点的浮层工具栏。按钮由适配当前选区的插件动态生成，带图标与悬停反馈。
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
            var content = new StackPanel { Orientation = Orientation.Horizontal };

            var icon = new TextBlock
            {
                Text = GetIcon(plugin.Id),
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

    /// <summary>按插件 Id 映射 Segoe MDL2 Assets 图标字符。</summary>
    private static string GetIcon(string pluginId) => pluginId switch
    {
        "builtin.copy" => "\uE8C8",      // 复制
        "builtin.smartopen" => "\uE8A7", // 链接 / 打开
        "builtin.search" => "\uE721",    // 搜索
        _ => "\uE71D",                    // 通用应用
    };

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
