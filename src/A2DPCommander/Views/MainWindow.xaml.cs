using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using BTAudioDriver.Localization;
using BTAudioDriver.ViewModels;

namespace BTAudioDriver.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly string _versionString;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        _versionString = $"{version?.Major}.{version?.Minor}.{version?.Build}";
        Title = $"A2DP Commander v{_versionString}";
        UpdateVersionText();

        Strings.LanguageChanged += (_, _) => UpdateVersionText();

        _viewModel.MinimizeToTrayRequested += OnMinimizeToTrayRequested;
    }

    private void UpdateVersionText()
    {
        VersionText.Text = string.Format(Strings.About_VersionFormat, _versionString);
    }

    private void OnMinimizeToTrayRequested(object? sender, EventArgs e)
    {
        Hide();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (WindowState == WindowState.Minimized)
        {
            Hide();
            WindowState = WindowState.Normal;
        }
    }

    public void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void CopyBtcButton_Click(object sender, RoutedEventArgs e)
    {
        const string btcAddress = "1BkYvFT8iBVG3GfTqkR2aBkABNkTrhYuja";
        System.Windows.Clipboard.SetText(btcAddress);

        if (sender is System.Windows.Controls.Button button)
        {
            var originalContent = button.Content;
            button.Content = Strings.Help_Copied;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (_, _) =>
            {
                button.Content = originalContent;
                timer.Stop();
            };
            timer.Start();
        }
    }

    private void GitHubLink_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Yumash/A2DP-Commander",
                UseShellExecute = true
            });
        }
        catch
        {
            System.Windows.Clipboard.SetText("https://github.com/Yumash/A2DP-Commander");
        }
    }
}
