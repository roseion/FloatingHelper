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

        Dispatcher.InvokeAsync(() => ShowToolbar(plugins, context));
    }

    private void ShowToolbar(IReadOnlyList<IPlugin> plugins, PluginContext context)
    {
        _toolbar?.Close();
        _toolbar = new ToolbarWindow(plugins, context);

        var pos = GetCursorPos();
        _toolbar.Left = pos.X + 12;
        _toolbar.Top = pos.Y + 12;
        _toolbar.Show();
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
