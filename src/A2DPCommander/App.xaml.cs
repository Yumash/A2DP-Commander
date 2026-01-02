using System.IO;
using BTAudioDriver.Localization;
using BTAudioDriver.Services;
using BTAudioDriver.ViewModels;
using BTAudioDriver.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace BTAudioDriver;

public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;
    private MainViewModel? _mainViewModel;
    private Views.MainWindow? _mainWindow;

    protected override async void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            ConfigureLogging();

            Log.Information("=== A2DP Commander Starting ===");

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            await InitializeLanguageAsync(settingsService);

            _mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

            _mainViewModel.ShowMainWindowRequested += OnShowMainWindowRequested;

            _mainWindow = new Views.MainWindow(_mainViewModel);
            _mainWindow.Show();

            Log.Information("Application started successfully");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Failed to start application");
            System.Windows.MessageBox.Show(
                $"Не удалось запустить приложение:\n{ex.Message}",
                Strings.AppName,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnShowMainWindowRequested(object? sender, EventArgs e)
    {
        _mainWindow?.ShowAndActivate();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Log.Information("=== A2DP Commander Shutting Down ===");

        _mainViewModel?.Dispose();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigureLogging()
    {
        var appFolder = AppDomain.CurrentDomain.BaseDirectory;
        var logsFolder = Path.Combine(appFolder, "logs");
        Directory.CreateDirectory(logsFolder);

        var logPath = Path.Combine(logsFolder, "btaudio-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 3,
                fileSizeLimitBytes: 1_000_000,
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IBluetoothService, BluetoothService>();
        services.AddSingleton<IAudioEndpointService, AudioEndpointService>();
        services.AddSingleton<IProfileManager, ProfileManager>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IProcessWatcherService, ProcessWatcherService>();
        services.AddSingleton<IAudioQualityService, AudioQualityService>();
        services.AddSingleton<IBluetoothCodecMonitor, BluetoothCodecMonitor>();

        services.AddSingleton<MainViewModel>();
    }

    private static async Task InitializeLanguageAsync(ISettingsService settingsService)
    {
        try
        {
            await settingsService.LoadAsync();

            var lang = settingsService.Settings.Language ?? "ru";
            Strings.CurrentLanguage = lang == "en" ? Language.English : Language.Russian;

            Log.Information("Language initialized: {Language}, CurrentLanguage={CurrentLang}",
                lang, Strings.CurrentLanguage);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load language settings, using default Russian");
            Strings.CurrentLanguage = Language.Russian;
        }
    }
}
