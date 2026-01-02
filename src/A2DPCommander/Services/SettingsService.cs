using System.IO;
using System.Text.Json;
using BTAudioDriver.Models;
using Microsoft.Win32;
using Serilog;

namespace BTAudioDriver.Services;

public class SettingsService : ISettingsService
{
    private static readonly ILogger Logger = Log.ForContext<SettingsService>();

    private const string AppName = "BTAudioDriver";
    private const string SettingsFileName = "settings.json";
    private const string RegistryRunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppSettings Settings { get; private set; } = new();

    public event EventHandler<AppSettings>? SettingsChanged;

    public SettingsService()
    {
        var appFolder = AppDomain.CurrentDomain.BaseDirectory;

        _settingsFilePath = Path.Combine(appFolder, SettingsFileName);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        Logger.Debug("Settings file path: {Path}", _settingsFilePath);
    }

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                Logger.Information("Settings file not found, using defaults");
                Settings = new AppSettings();
                await SaveAsync();
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();

            Settings.AutoStart = IsAutoStartEnabled();

            Logger.Information("Settings loaded: Device={Device}, AutoStart={AutoStart}, DefaultMode={Mode}, Language={Language}",
                Settings.DefaultDeviceName, Settings.AutoStart, Settings.DefaultMode, Settings.Language);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load settings");
            Settings = new AppSettings();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            Logger.Information("Settings saved");
            SettingsChanged?.Invoke(this, Settings);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save settings");
            throw;
        }
    }

    public void SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey, writable: true);
            if (key == null)
            {
                Logger.Warning("Cannot open Run registry key");
                return;
            }

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    Logger.Warning("Cannot determine executable path");
                    return;
                }

                key.SetValue(AppName, $"\"{exePath}\"");
                Logger.Information("Auto-start enabled: {Path}", exePath);
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
                Logger.Information("Auto-start disabled");
            }

            Settings.AutoStart = enable;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set auto-start");
            throw;
        }
    }

    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryRunKey);
            if (key == null) return false;

            var value = key.GetValue(AppName);
            return value != null;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to check auto-start status");
            return false;
        }
    }
}
