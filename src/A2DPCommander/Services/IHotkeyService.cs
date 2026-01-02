using BTAudioDriver.Models;

namespace BTAudioDriver.Services;

public interface IHotkeyService : IDisposable
{
    event EventHandler? ToggleModeRequested;

    event EventHandler? MusicModeRequested;

    event EventHandler? CallsModeRequested;

    bool Register(HotkeyConfig? toggleHotkey);

    void Unregister();
}
