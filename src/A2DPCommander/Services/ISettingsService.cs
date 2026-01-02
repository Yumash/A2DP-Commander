using BTAudioDriver.Models;

namespace BTAudioDriver.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }

    Task LoadAsync();

    Task SaveAsync();

    void SetAutoStart(bool enable);

    bool IsAutoStartEnabled();

    event EventHandler<AppSettings>? SettingsChanged;
}
