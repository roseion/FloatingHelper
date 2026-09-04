using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FloatingHelper.Plugins.SaveToFile;

/// <summary>
/// 「保存」插件设置窗口：选择 / 输入要保存到的本地文本文档。
/// 纯代码构建（不使用 XAML），避免动态加载程序集解析嵌入资源带来的不确定性，
/// 保证作为外部插件 DLL 被加载时设置窗口可稳定弹出。
/// </summary>
public sealed class SaveSettingsWindow : Window
{
    private readonly TextBox _pathBox;

    /// <summary>用户最终选择的目标文本文档路径（点击「确定」后有效）。</summary>
    public string TargetFilePath => _pathBox.Text.Trim();

    public SaveSettingsWindow(string? currentPath)
    {
        _pathBox = new TextBox
        {
            Padding = new Thickness(6, 4, 6, 4),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            _pathBox.Text = currentPath;
        }

        Title = "保存 · 设置";
        Width = 460;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;

        var browseButton = new Button
        {
            Content = "浏览…",
            Padding = new Thickness(12, 4, 12, 4),
            Margin = new Thickness(8, 0, 0, 0),
        };
        browseButton.Click += OnBrowseClick;

        var browsePanel = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
        DockPanel.SetDock(browseButton, Dock.Right);
        browsePanel.Children.Add(browseButton);
        browsePanel.Children.Add(_pathBox);

        var hint = new TextBlock
        {
            Text = "选中文字将被追加保存到该文件（每段另起一行），文件不存在时会自动创建。",
            Foreground = System.Windows.Media.Brushes.Gray,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0),
        };

        var okButton = new Button
        {
            Content = "确定",
            Padding = new Thickness(16, 6, 16, 6),
            IsDefault = true,
            Margin = new Thickness(0, 0, 8, 0),
        };
        okButton.Click += OnOkClick;

        var cancelButton = new Button
        {
            Content = "取消",
            Padding = new Thickness(16, 6, 16, 6),
            IsCancel = true,
        };
        cancelButton.Click += (_, _) => DialogResult = false;

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0),
        };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        var layout = new StackPanel { Margin = new Thickness(20) };
        layout.Children.Add(new TextBlock { Text = "保存到文本文档", FontWeight = FontWeights.SemiBold });
        layout.Children.Add(browsePanel);
        layout.Children.Add(hint);
        layout.Children.Add(buttons);

        Content = layout;
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要保存到的文本文档",
            Filter = "文本文档 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            // 允许直接输入新文件名：目标文件不存在时插件会自动创建。
            CheckFileExists = false,
            Multiselect = false,
        };

        if (!string.IsNullOrWhiteSpace(_pathBox.Text))
        {
            var directory = Path.GetDirectoryName(_pathBox.Text);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                dialog.InitialDirectory = directory;
            }
        }

        if (dialog.ShowDialog(this) == true)
        {
            _pathBox.Text = dialog.FileName;
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TargetFilePath))
        {
            MessageBox.Show(this, "请先选择要保存到的文本文档。", "保存 · 设置",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
