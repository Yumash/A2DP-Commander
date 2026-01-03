using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using BTAudioDriver.Localization;
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
    private string _deviceStatus = "";

    [ObservableProperty]
    private string _currentMode = "";

    [ObservableProperty]
    private string _bluetoothInfo = "";

    [ObservableProperty]
    private string _audioInfo = "";

    [ObservableProperty]
    private string _codecInfo = "";

    [ObservableProperty]
    private string _settingsInfo = "";

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
                DeviceStatus = $"{device.Name} — {Strings.Device_Connected}";
                BluetoothInfo = $"ID: {device.Id}\n" +
                               $"MAC: {device.MacAddress}\n" +
                               $"{Strings.Diag_A2dpSupported}: {(device.SupportsA2dp ? Strings.Diag_Yes : Strings.Diag_No)}\n" +
                               $"{Strings.Diag_HfpSupported}: {(device.SupportsHfp ? Strings.Diag_Yes : Strings.Diag_No)}\n" +
                               $"{Strings.Diag_AvrcpSupported}: {(device.SupportsAvrcp ? Strings.Diag_Yes : Strings.Diag_No)}";
            }
            else
            {
                DeviceStatus = Strings.Diag_NoConnectedDevices;
                BluetoothInfo = $"{Strings.Diag_PairedDevices}: {devices.Count}";
            }

            var deviceName = _settingsService.Settings.DefaultDeviceName;
            var state = await _profileManager.GetDeviceProfileStateAsync(deviceName);
            CurrentMode = state?.CurrentMode.ToString() ?? Strings.Status_Unknown;

            var playbackEndpoints = _audioService.GetPlaybackEndpoints();
            var btEndpoints = _audioService.GetBluetoothEndpoints();

            AudioInfo = $"{Strings.Diag_PlaybackDevices}: {playbackEndpoints.Count}\n" +
                       $"Bluetooth: {btEndpoints.Count}\n" +
                       string.Join("\n", btEndpoints.Select(e => $"  - {e.FriendlyName} ({e.BluetoothProfile})"));

            if (_audioQualityService != null)
            {
                var qualityInfo = _audioQualityService.GetCurrentQualityInfo(deviceName);
                if (qualityInfo != null)
                {
                    CodecInfo = $"{Strings.Diag_CodecLabel}: {qualityInfo.CodecName}\n" +
                               $"{Strings.Diag_Frequency}: {qualityInfo.SampleRate / 1000.0:F1} kHz\n" +
                               $"{Strings.Diag_BitDepth}: {qualityInfo.BitDepth} bit\n" +
                               $"{Strings.Diag_Channels}: {qualityInfo.Channels}\n" +
                               $"{Strings.Diag_Bitrate}: ~{qualityInfo.Bitrate} kbps\n" +
                               $"{(qualityInfo.EnhancementsEnabled ? Strings.Diag_EnhancementsEnabled : Strings.Diag_EnhancementsDisabled)}\n" +
                               $"{Strings.Codec_SupportedCodecs} {string.Join(", ", qualityInfo.SupportedCodecs)}";
                }
                else
                {
                    CodecInfo = Strings.Diag_CodecInfoUnavailable;
                }
            }
            else
            {
                CodecInfo = Strings.Diag_ServiceNotInit;
            }

            var settings = _settingsService.Settings;
            SettingsInfo = $"{Strings.Diag_Device}: {settings.DefaultDeviceName}\n" +
                          $"{Strings.Settings_DefaultMode}: {settings.DefaultMode}\n" +
                          $"{Strings.Diag_AutoStart}: {(settings.AutoStart ? Strings.Diag_Yes : Strings.Diag_No)}\n" +
                          $"{Strings.Diag_AutoSwitch}: {(settings.AutoSwitchByApp ? Strings.Diag_Yes : Strings.Diag_No)}\n" +
                          $"{Strings.Diag_Notifications}: {(settings.ShowNotifications ? Strings.Diag_Yes : Strings.Diag_No)}";

            await LoadRecentLogsAsync();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to refresh diagnostics");
            DeviceStatus = Strings.Diag_ErrorGettingData;
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
                LogEntries.Add(Strings.Diag_NoLogsFound);
                return;
            }

            var latestLog = logDir.GetFiles("btaudio-*.log")
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();

            if (latestLog == null)
            {
                LogEntries.Clear();
                LogEntries.Add(Strings.Diag_NoLogsFound);
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
            LogEntries.Add($"{Strings.Status_Error}: {ex.Message}");
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
                    Strings.Notification_LogsFolderNotFound,
                    Strings.AppName,
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
                FileName = $"A2DPCommander_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".txt",
                Filter = "Text files (*.txt)|*.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                var report = GenerateDiagnosticsReport();
                await File.WriteAllTextAsync(dialog.FileName, report);

                System.Windows.MessageBox.Show(
                    $"{dialog.FileName}",
                    Strings.AppName,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to export diagnostics");
            System.Windows.MessageBox.Show(
                $"{Strings.Status_Error}: {ex.Message}",
                Strings.AppName,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private string GenerateDiagnosticsReport()
    {
        return $"""
            === A2DP Commander Diagnostics ===
            Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

            === Device ===
            {DeviceStatus}
            Mode: {CurrentMode}

            === Bluetooth ===
            {BluetoothInfo}

            === Audio ===
            {AudioInfo}

            === Codec ===
            {CodecInfo}

            === Settings ===
            {SettingsInfo}

            === Logs ===
            {string.Join("\n", LogEntries)}
            """;
    }
}
