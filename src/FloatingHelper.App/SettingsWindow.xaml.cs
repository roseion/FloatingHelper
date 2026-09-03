using System.Windows;
using FloatingHelper.Core.Actions;
using FloatingHelper.Core.Configuration;
using FloatingHelper.Core.Plugins;

namespace FloatingHelper.App;

/// <summary>
/// 插件管理设置窗口：插件列表（启停）、外部插件加载 / 卸载、搜索模板配置。
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly PluginManager _manager;
    private readonly AppSettings _settings;
    private readonly Action _persist;
    private readonly List<PluginListItem> _items = new();

    public SettingsWindow(PluginManager manager, AppSettings settings, Action persist)
    {
        InitializeComponent();
        _manager = manager;
        _settings = settings;
        _persist = persist;
        SearchTemplateBox.Text = settings.SearchTemplate;
        RestoreDisplayMode(settings.DisplayMode);
        RefreshList();
    }

    /// <summary>按当前配置恢复工具栏显示模式的选中状态。</summary>
    private void RestoreDisplayMode(ToolbarDisplayMode mode)
    {
        switch (mode)
        {
            case ToolbarDisplayMode.IconOnly:
                ModeIconOnly.IsChecked = true;
                break;
            case ToolbarDisplayMode.TextOnly:
                ModeTextOnly.IsChecked = true;
                break;
            default:
                ModeIconText.IsChecked = true;
                break;
        }
    }

    /// <summary>显示模式单选变化时同步到配置。</summary>
    private void OnDisplayModeChanged(object sender, RoutedEventArgs e)
    {
        if (ModeIconText.IsChecked == true)
        {
            _settings.DisplayMode = ToolbarDisplayMode.IconAndText;
        }
        else if (ModeIconOnly.IsChecked == true)
        {
            _settings.DisplayMode = ToolbarDisplayMode.IconOnly;
        }
        else if (ModeTextOnly.IsChecked == true)
        {
            _settings.DisplayMode = ToolbarDisplayMode.TextOnly;
        }
    }

    private void RefreshList()
    {
        _items.Clear();
        foreach (var plugin in _manager.Plugins)
        {
            _items.Add(new PluginListItem(plugin, _manager.IsBuiltin(plugin)));
        }

        PluginList.ItemsSource = null;
        PluginList.ItemsSource = _items;
    }

    private void OnPluginToggle(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not PluginListItem item)
        {
            return;
        }

        _manager.SetEnabled(item.Plugin, item.IsEnabled);
        _persist();
    }

    private void OnLoadClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择插件程序集",
            Filter = "插件程序集 (*.dll)|*.dll",
            Multiselect = true,
        };

        if (dialog.ShowDialog(this) == true)
        {
            foreach (var file in dialog.FileNames)
            {
                _manager.LoadFromFile(file);
            }

            _persist();
            RefreshList();
        }
    }

    private void OnUnloadClick(object sender, RoutedEventArgs e)
    {
        if (PluginList.SelectedItem is not PluginListItem item)
        {
            MessageBox.Show(this, "请先选择一个插件。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (item.IsBuiltin)
        {
            MessageBox.Show(this, "内置插件不可卸载，仅可启用 / 禁用。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_manager.Unload(item.Plugin))
        {
            _persist();
            RefreshList();
        }
    }

    private void OnRestoreDefaultClick(object sender, RoutedEventArgs e)
    {
        _settings.SearchTemplate = SearchUrlBuilder.DefaultSearchTemplate;
        foreach (var plugin in _manager.Plugins)
        {
            _manager.SetEnabled(plugin, true);
        }

        SearchTemplateBox.Text = _settings.SearchTemplate;
        _persist();
        RefreshList();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not PluginListItem item)
        {
            return;
        }

        if (!item.HasSettings)
        {
            return;
        }

        try
        {
            item.Plugin.ShowSettings(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"打开插件设置失败：{ex.Message}", "浮动助手",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _settings.SearchTemplate = SearchTemplateBox.Text.Trim();
        _persist();
        MessageBox.Show(this, "设置已保存。", "浮动助手", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

/// <summary>设置窗口插件列表项的视图模型。</summary>
public sealed class PluginListItem
{
    public PluginListItem(IPlugin plugin, bool isBuiltin)
    {
        Plugin = plugin;
        IsBuiltin = isBuiltin;
    }

    public IPlugin Plugin { get; }

    public bool IsBuiltin { get; }

    public string Name => Plugin.Name;

    public string Description => Plugin.Description;

    public string Source => IsBuiltin ? "内置" : "外部 DLL";

    public bool HasSettings => Plugin.HasSettings;

    public bool IsEnabled
    {
        get => Plugin.IsEnabled;
        set => Plugin.IsEnabled = value;
    }
}
