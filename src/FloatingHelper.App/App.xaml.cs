using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using FloatingHelper.Core.Configuration;
using FloatingHelper.Core.Logging;
using FloatingHelper.Core.Plugins;
using FloatingHelper.Core.Selection;
using FloatingHelper.Plugins.Builtin;
using Forms = System.Windows.Forms;
using Microsoft.Win32;

namespace FloatingHelper.App;

/// <summary>
/// 浮动助手入口：加载配置与插件、托盘常驻、开机自启、全局鼠标钩子，调度浮层工具栏与设置窗口。
/// </summary>
public partial class App : System.Windows.Application
{
    private const string AutoStartKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AutoStartValueName = "FloatingHelper";
    private const string SingleInstanceMutexName = @"Local\FloatingHelper.SingleInstance";

    private readonly PluginManager _pluginManager = new();
    private AppSettings _settings = new();
    private string _settingsPath = string.Empty;
    private Mutex? _singleInstance;
    private GlobalMouseHook? _hook;
    private ToolbarWindow? _toolbar;
    private ResultPopupWindow? _resultPopup;
    private SettingsWindow? _settingsWindow;
    private Forms.NotifyIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Logger.EnsureInitialized();
        Logger.Info("应用启动");

        // 单实例保护：已有实例在运行则直接退出。
        _singleInstance = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            Logger.Warn("检测到已有实例运行，退出");
            Shutdown();
            return;
        }

        LoadSettingsAndPlugins();

        // 开机自启状态与配置保持一致。
        SetAutoStart(_settings.AutoStart);

        SetupTray();
        SetupHook();

        // 休眠恢复 / 会话解锁后重连全局钩子（低层钩子在系统事件后可能失效）。
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    private void SetupHook()
    {
        _hook = new GlobalMouseHook();
        _hook.SelectionFinished += OnSelectionFinished;
        _hook.MouseDown += OnMouseDown;
        _hook.Start();
    }

    private void ReconnectHook()
    {
        Logger.Info("重连全局钩子");
        _hook?.Dispose();
        SetupHook();
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Dispatcher.InvokeAsync(ReconnectHook);
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock)
        {
            Dispatcher.InvokeAsync(ReconnectHook);
        }
    }

    private void LoadSettingsAndPlugins()
    {
        _settingsPath = SettingsStore.GetDefaultPath();
        _settings = SettingsStore.Load(_settingsPath);

        // 注册内置插件（复制 / 智能打开 / 搜索），搜索插件使用配置的模板。
        _pluginManager.AddBuiltin(new CopyPlugin());
        _pluginManager.AddBuiltin(new SmartOpenPlugin());
        _pluginManager.AddBuiltin(new SearchPlugin { SearchTemplate = _settings.SearchTemplate });

        // 站点直达插件：站内搜索（小红书 / 知乎 / 微博）。
        _pluginManager.AddBuiltin(new XiaohongshuSearchPlugin());
        _pluginManager.AddBuiltin(new ZhihuSearchPlugin());
        _pluginManager.AddBuiltin(new WeiboSearchPlugin());

        // 站点直达插件：AI 提问（豆包 / 元宝 / DeepSeek）。
        _pluginManager.AddBuiltin(new DoubaoAskPlugin());
        _pluginManager.AddBuiltin(new YuanbaoAskPlugin());
        _pluginManager.AddBuiltin(new DeepSeekAskPlugin());

        // 恢复插件启停状态，并为缺省配置补齐默认值。
        foreach (var plugin in _pluginManager.Plugins)
        {
            if (_settings.Plugins.TryGetValue(plugin.Id, out var pluginSetting))
            {
                _pluginManager.SetEnabled(plugin, pluginSetting.Enabled);
            }
            else
            {
                _settings.Plugins[plugin.Id] = new PluginSetting { Enabled = plugin.IsEnabled };
            }
        }

        // 按配置记录的路径加载外部插件，保证其启停状态可恢复。
        foreach (var (id, pluginSetting) in _settings.Plugins)
        {
            if (pluginSetting.Path is not null
                && File.Exists(pluginSetting.Path)
                && _pluginManager.GetPlugin(id) is null)
            {
                _pluginManager.LoadFromFile(pluginSetting.Path);
            }
        }

        // 插件目录兜底（部署目录 plugins，放置 *.dll 即可扩展）。
        _pluginManager.LoadFromDirectory(Path.Combine(AppContext.BaseDirectory, "plugins"));

        // 插件状态变更即持久化。
        _pluginManager.StateChanged += (_, _) => SaveSettings();
        SaveSettings();
    }

    private void SaveSettings()
    {
        foreach (var plugin in _pluginManager.Plugins)
        {
            if (!_settings.Plugins.TryGetValue(plugin.Id, out var pluginSetting))
            {
                pluginSetting = new PluginSetting();
                _settings.Plugins[plugin.Id] = pluginSetting;
            }

            pluginSetting.Enabled = plugin.IsEnabled;
            pluginSetting.Path = _pluginManager.GetExternalPath(plugin);
        }

        // 搜索模板变更同步到搜索插件。
        if (_pluginManager.GetPlugin("builtin.search") is SearchPlugin searchPlugin)
        {
            searchPlugin.SearchTemplate = _settings.SearchTemplate;
        }

        SettingsStore.Save(_settings, _settingsPath);
    }

    private void OnSelectionFinished()
    {
        var selection = SelectionCaptureService.TryCapture();
        if (selection is null)
        {
            return;
        }

        // UIA 选区边界为物理像素，转换为 DIP 传给插件。
        Rect? selectionBounds = selection.Bounds is Rect physicalBounds
            ? DisplayHelper.PhysicalRectToDip(physicalBounds)
            : null;

        var context = new PluginContext
        {
            SelectedText = selection.Text,
            SourceProcessName = selection.ProcessName,
            SelectionBounds = selectionBounds,
        };

        var plugins = _pluginManager.GetApplicablePlugins(context);
        if (plugins.Count == 0)
        {
            return;
        }

        // 在拖选结束的触发时刻记录鼠标位置（物理像素 → DIP），避免异步显示时位置漂移。
        var physical = GetCursorPos();
        var position = DisplayHelper.PhysicalToDip(physical.X, physical.Y);
        Dispatcher.InvokeAsync(() => ShowToolbar(plugins, context, position));
    }

    /// <summary>左键按下时，若点击在工具栏或结果浮层外区域则关闭它们。</summary>
    private void OnMouseDown((int X, int Y) physicalPoint)
    {
        var (x, y) = DisplayHelper.PhysicalToDip(physicalPoint.X, physicalPoint.Y);

        if (_toolbar is not null && _toolbar.IsLoaded && !_toolbar.IsPointOver(x, y))
        {
            Dispatcher.InvokeAsync(() => _toolbar?.Close());
        }

        if (_resultPopup is not null && _resultPopup.IsLoaded && !_resultPopup.IsPointOver(x, y))
        {
            Dispatcher.InvokeAsync(() => _resultPopup?.Close());
        }
    }

    private void ShowToolbar(IReadOnlyList<IPlugin> plugins, PluginContext context, (double X, double Y) position)
    {
        _toolbar?.Close();
        _toolbar = new ToolbarWindow(plugins, context);
        _toolbar.PluginResultReady += OnPluginResultReady;

        // 工具栏出现在鼠标位置附近（右下方偏移），并按鼠标所在屏幕工作区收敛，避免越界。
        _toolbar.Left = position.X + 12;
        _toolbar.Top = position.Y + 12;
        _toolbar.Show();
        _toolbar.UpdateLayout();

        var area = DisplayHelper.GetWorkAreaDip(position.X, position.Y);
        var left = Math.Clamp(_toolbar.Left, area.Left + 4, area.Right - _toolbar.ActualWidth - 4);
        var top = Math.Clamp(_toolbar.Top, area.Top + 4, area.Bottom - _toolbar.ActualHeight - 4);
        _toolbar.Left = left;
        _toolbar.Top = top;
    }

    /// <summary>插件返回文本结果时，在选区下方显示结果浮层。</summary>
    private void OnPluginResultReady(string result, PluginContext context)
    {
        Logger.Info($"[结果浮层] 收到插件结果，长度={result.Length}，选区位置={context.SelectionBounds}");
        Dispatcher.InvokeAsync(() => ShowResultPopup(result, context));
    }

    private void ShowResultPopup(string text, PluginContext context)
    {
        _resultPopup?.Close();
        _resultPopup = new ResultPopupWindow(text);

        // 优先显示在选区下方，无选区位置时回退到鼠标位置下方。
        double left, top;
        if (context.SelectionBounds is Rect bounds)
        {
            left = bounds.Left;
            top = bounds.Bottom + 6;
        }
        else
        {
            var physical = GetCursorPos();
            var pos = DisplayHelper.PhysicalToDip(physical.X, physical.Y);
            left = pos.X;
            top = pos.Y + 24;
        }

        _resultPopup.Left = left;
        _resultPopup.Top = top;
        _resultPopup.Show();
        _resultPopup.UpdateLayout();

        var area = DisplayHelper.GetWorkAreaDip(left, top);
        _resultPopup.Left = Math.Clamp(_resultPopup.Left, area.Left + 4, area.Right - _resultPopup.ActualWidth - 4);
        _resultPopup.Top = Math.Clamp(_resultPopup.Top, area.Top + 4, area.Bottom - _resultPopup.ActualHeight - 4);

        Logger.Info($"[结果浮层] 已显示，位置=({_resultPopup.Left:F0},{_resultPopup.Top:F0})，大小={_resultPopup.ActualWidth:F0}x{_resultPopup.ActualHeight:F0}");
    }

    /// <summary>从程序集资源加载应用图标，失败时回退到系统默认图标。</summary>
    private static System.Drawing.Icon LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/app.ico", UriKind.Absolute);
            var sri = GetResourceStream(uri);
            if (sri is not null)
            {
                return new System.Drawing.Icon(sri.Stream);
            }
        }
        catch
        {
            // 回退到系统默认图标。
        }

        return System.Drawing.SystemIcons.Application;
    }

    private void SetupTray()
    {
        _tray = new Forms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "浮动助手",
            Visible = true,
        };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("浮动助手 · 划词工具栏", null, (_, _) => { }).Enabled = false;

        var settingsItem = new Forms.ToolStripMenuItem("插件管理…");
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        var autoStartItem = new Forms.ToolStripMenuItem("开机自启") { Checked = _settings.AutoStart };
        autoStartItem.Click += (_, _) =>
        {
            _settings.AutoStart = !autoStartItem.Checked;
            autoStartItem.Checked = _settings.AutoStart;
            SetAutoStart(_settings.AutoStart);
            SaveSettings();
        };
        menu.Items.Add(autoStartItem);

        var aboutItem = new Forms.ToolStripMenuItem("关于");
        aboutItem.Click += (_, _) => OpenAbout();
        menu.Items.Add(aboutItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => Shutdown();
        menu.Items.Add(exitItem);

        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => OpenSettings();
    }

    private void OpenSettings()
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (_settingsWindow is null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new SettingsWindow(_pluginManager, _settings, SaveSettings)
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                };
                _settingsWindow.Closed += (_, _) => _settingsWindow = null;
                _settingsWindow.Show();
            }

            _settingsWindow.Activate();
        });
    }

    private void OpenAbout()
    {
        Dispatcher.InvokeAsync(() =>
        {
            var about = new AboutWindow();
            about.ShowDialog();
        });
    }

    private static void SetAutoStart(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(AutoStartKeyPath);
            if (key is null)
            {
                return;
            }

            if (enabled)
            {
                var exePath = Environment.ProcessPath
                    ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                    ?? string.Empty;
                key.SetValue(AutoStartValueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AutoStartValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("设置开机自启失败", ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        _hook?.Dispose();
        _toolbar?.Close();
        _resultPopup?.Close();
        _tray?.Dispose();
        _pluginManager.Dispose();
        _singleInstance?.Dispose();
        Logger.Info("应用退出");
        base.OnExit(e);
    }

    private static (int X, int Y) GetCursorPos()
    {
        return GetCursorPos(out var pt) ? (pt.X, pt.Y) : (0, 0);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
