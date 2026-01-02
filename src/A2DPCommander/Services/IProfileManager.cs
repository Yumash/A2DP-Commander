using BTAudioDriver.Models;

namespace BTAudioDriver.Services;

public interface IProfileManager : IDisposable
{
    event EventHandler<DeviceProfileState>? ProfileModeChanged;

    Task<DeviceProfileState?> GetDeviceProfileStateAsync(string deviceName, CancellationToken cancellationToken = default);

    Task<bool> SetMusicModeAsync(string deviceName, CancellationToken cancellationToken = default);

    Task<bool> SetCallsModeAsync(string deviceName, CancellationToken cancellationToken = default);

    Task<ProfileMode> ToggleModeAsync(string deviceName, CancellationToken cancellationToken = default);

    bool RequiresAdminRights { get; }

    bool IsRunningAsAdmin { get; }
}
