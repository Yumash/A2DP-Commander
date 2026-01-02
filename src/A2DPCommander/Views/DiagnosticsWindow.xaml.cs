using System.Windows;
using BTAudioDriver.ViewModels;

namespace BTAudioDriver.Views;

public partial class DiagnosticsWindow : Window
{
    public DiagnosticsWindow(DiagnosticsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
