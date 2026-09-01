using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using FloatingHelper.Core.Plugins;
using FloatingHelper.Core.Selection;
using FloatingHelper.Plugins.Builtin;
using Forms = System.Windows.Forms;

namespace FloatingHelper.App;

/// <summary>
/// 浮动助手入口：初始化插件、托盘常驻与全局鼠标钩子，调度浮层工具栏。
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly PluginManager _pluginManager = new();
    private GlobalMouseHook? _hook;
    private ToolbarWindow? _toolbar;
    private Forms.NotifyIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 注册内置插件（复制 / 智能打开 / 搜索）。
        _pluginManager.AddBuiltin(new CopyPlugin());
        _pluginManager.AddBuiltin(new SmartOpenPlugin());
        _pluginManager.AddBuiltin(new SearchPlugin());

        // 尝试从插件目录加载外部插件（放置 *.dll 即可扩展）。
        var pluginDir = Path.Combine(AppContext.BaseDirectory, "plugins");
        var externalLoaded = _pluginManager.LoadFromDirectory(pluginDir);

        SetupTray();
        _hook = new GlobalMouseHook();
        _hook.SelectionFinished += OnSelectionFinished;
        _hook.Start();
    }

    private void OnSelectionFinished()
    {
        var selection = SelectionCaptureService.TryCapture();
        if (selection is null)
        {
            return;
        }

        var context = new PluginContext
        {
            SelectedText = selection.Text,
            SourceProcessName = selection.ProcessName,
        };

        var plugins = _pluginManager.GetApplicablePlugins(context);
        if (plugins.Count == 0)
        {
            return;
        }

        // 在拖选结束的触发时刻记录鼠标位置，避免异步显示时位置漂移。
        var position = GetCursorPos();
        Dispatcher.InvokeAsync(() => ShowToolbar(plugins, context, position));
    }

    private void ShowToolbar(IReadOnlyList<IPlugin> plugins, PluginContext context, (double X, double Y) position)
    {
        _toolbar?.Close();
        _toolbar = new ToolbarWindow(plugins, context);

        // 工具栏出现在鼠标位置附近（右下方偏移），并按实际尺寸收敛到屏幕工作区，避免越界。
        _toolbar.Left = position.X + 12;
        _toolbar.Top = position.Y + 12;
        _toolbar.Show();
        _toolbar.UpdateLayout();

        var area = SystemParameters.WorkArea;
        var left = Math.Clamp(_toolbar.Left, area.Left + 4, area.Right - _toolbar.ActualWidth - 4);
        var top = Math.Clamp(_toolbar.Top, area.Top + 4, area.Bottom - _toolbar.ActualHeight - 4);
        _toolbar.Left = left;
        _toolbar.Top = top;
    }

    private void SetupTray()
    {
        _tray = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "浮动助手",
            Visible = true,
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("浮动助手 · 划词工具栏", null, (_, _) => { }).Enabled = false;
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Shutdown());
        _tray.ContextMenuStrip = menu;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hook?.Dispose();
        _tray?.Dispose();
        _pluginManager.Dispose();
        base.OnExit(e);
    }

    private static (double X, double Y) GetCursorPos()
    {
        return GetCursorPos(out var pt) ? (pt.X, pt.Y) : (0, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public double X;
        public double Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
