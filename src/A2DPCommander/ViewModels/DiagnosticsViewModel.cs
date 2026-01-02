using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using BTAudioDriver.Models;
using BTAudioDriver.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace BTAudioDriver.ViewModels;

public partial class DiagnosticsViewModel : ObservableObject
{
    private static readonly ILogger Logger = Log.ForContext<DiagnosticsViewModel>();

    private readonly IBluetoothService _bluetoothService;
    private readonly IAudioEndpointService _audioService;
    private readonly IProfileManager _profileManager;
    private readonly ISettingsService _settingsService;
    private readonly IAudioQualityService? _audioQualityService;

    [ObservableProperty]
    private string _deviceStatus = "Неизвестно";

    [ObservableProperty]
    private string _currentMode = "Неизвестно";

    [ObservableProperty]
    private string _bluetoothInfo = "Загрузка...";

    [ObservableProperty]
    private string _audioInfo = "Загрузка...";

    [ObservableProperty]
    private string _codecInfo = "Загрузка...";

    [ObservableProperty]
    private string _settingsInfo = "Загрузка...";

    [ObservableProperty]
    private ObservableCollection<string> _logEntries = new();

    [ObservableProperty]
    private bool _isRefreshing;

    public string LogFilePath { get; }

    public DiagnosticsViewModel(
        IBluetoothService bluetoothService,
        IAudioEndpointService audioService,
        IProfileManager profileManager,
        ISettingsService settingsService,
        IAudioQualityService? audioQualityService = null)
    {
        _bluetoothService = bluetoothService;
        _audioService = audioService;
        _profileManager = profileManager;
        _settingsService = settingsService;
        _audioQualityService = audioQualityService;

        LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BTAudioDriver",
            "logs");

        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsRefreshing) return;
        IsRefreshing = true;

        try
        {
            var devices = await _bluetoothService.GetPairedAudioDevicesAsync();
            var connectedDevices = devices.Where(d => d.IsConnected).ToList();

            if (connectedDevices.Any())
            {
                var device = connectedDevices.First();
                DeviceStatus = $"{device.Name} — Подключено";
                BluetoothInfo = $"ID: {device.Id}\n" +
                               $"MAC: {device.MacAddress}\n" +
                               $"A2DP: {(device.SupportsA2dp ? "Да" : "Нет")}\n" +
                               $"HFP: {(device.SupportsHfp ? "Да" : "Нет")}\n" +
                               $"AVRCP: {(device.SupportsAvrcp ? "Да" : "Нет")}";
            }
            else
            {
                DeviceStatus = "Нет подключённых устройств";
                BluetoothInfo = $"Сопряжённых устройств: {devices.Count}";
            }

            var deviceName = _settingsService.Settings.DefaultDeviceName;
            var state = await _profileManager.GetDeviceProfileStateAsync(deviceName);
            CurrentMode = state?.CurrentMode.ToString() ?? "Неизвестно";

            var playbackEndpoints = _audioService.GetPlaybackEndpoints();
            var btEndpoints = _audioService.GetBluetoothEndpoints();

            AudioInfo = $"Устройств воспроизведения: {playbackEndpoints.Count}\n" +
                       $"Bluetooth устройств: {btEndpoints.Count}\n" +
                       string.Join("\n", btEndpoints.Select(e => $"  - {e.FriendlyName} ({e.BluetoothProfile})"));

            if (_audioQualityService != null)
            {
                var qualityInfo = _audioQualityService.GetCurrentQualityInfo(deviceName);
                if (qualityInfo != null)
                {
                    CodecInfo = $"Кодек: {qualityInfo.CodecName}\n" +
                               $"Частота: {qualityInfo.SampleRate / 1000.0:F1} kHz\n" +
                               $"Глубина: {qualityInfo.BitDepth} bit\n" +
                               $"Каналы: {qualityInfo.Channels}\n" +
                               $"Битрейт: ~{qualityInfo.Bitrate} kbps\n" +
                               $"Улучшения Windows: {(qualityInfo.EnhancementsEnabled ? "Включены" : "Отключены")}\n" +
                               $"Поддерживаемые кодеки: {string.Join(", ", qualityInfo.SupportedCodecs)}";
                }
                else
                {
                    CodecInfo = "Информация о кодеке недоступна";
                }
            }
            else
            {
                CodecInfo = "Сервис качества звука не инициализирован";
            }

            var settings = _settingsService.Settings;
            SettingsInfo = $"Устройство: {settings.DefaultDeviceName}\n" +
                          $"Режим по умолчанию: {settings.DefaultMode}\n" +
                          $"Автозапуск: {(settings.AutoStart ? "Да" : "Нет")}\n" +
                          $"Автопереключение: {(settings.AutoSwitchByApp ? "Да" : "Нет")}\n" +
                          $"Уведомления: {(settings.ShowNotifications ? "Да" : "Нет")}";

            await LoadRecentLogsAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to refresh diagnostics");
            DeviceStatus = "Ошибка получения данных";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadRecentLogsAsync()
    {
        try
        {
            var logDir = new DirectoryInfo(LogFilePath);
            if (!logDir.Exists)
            {
                LogEntries.Clear();
                LogEntries.Add("Логи не найдены");
                return;
            }

            var latestLog = logDir.GetFiles("btaudio-*.log")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (latestLog == null)
            {
                LogEntries.Clear();
                LogEntries.Add("Логи не найдены");
                return;
            }

            var lines = await File.ReadAllLinesAsync(latestLog.FullName);
            var recentLines = lines.TakeLast(50).ToList();

            LogEntries.Clear();
            foreach (var line in recentLines)
            {
                LogEntries.Add(line);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load logs");
            LogEntries.Clear();
            LogEntries.Add($"Ошибка чтения логов: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            if (Directory.Exists(LogFilePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = LogFilePath,
                    UseShellExecute = true
                });
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "Папка с логами не найдена",
                    "BT Audio Driver",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open log folder");
        }
    }

    [RelayCommand]
    private async Task ExportDiagnosticsAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"BTAudioDriver_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".txt",
                Filter = "Text files (*.txt)|*.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                var report = GenerateDiagnosticsReport();
                await File.WriteAllTextAsync(dialog.FileName, report);

                System.Windows.MessageBox.Show(
                    $"Диагностика сохранена в:\n{dialog.FileName}",
                    "BT Audio Driver",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to export diagnostics");
            System.Windows.MessageBox.Show(
                $"Ошибка экспорта: {ex.Message}",
                "BT Audio Driver",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private string GenerateDiagnosticsReport()
    {
        return $"""
            === BT Audio Driver Diagnostics ===
            Дата: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

            === Устройство ===
            {DeviceStatus}
            Текущий режим: {CurrentMode}

            === Bluetooth ===
            {BluetoothInfo}

            === Аудио ===
            {AudioInfo}

            === Качество звука ===
            {CodecInfo}

            === Настройки ===
            {SettingsInfo}

            === Последние логи ===
            {string.Join("\n", LogEntries)}
            """;
    }
}
