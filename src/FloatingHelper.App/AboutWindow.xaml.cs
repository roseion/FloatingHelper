using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace FloatingHelper.App;

/// <summary>关于窗口：产品信息、主页与 QQ。</summary>
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void OnOkClick(object sender, RoutedEventArgs e) => Close();

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
