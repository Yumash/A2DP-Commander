using BTAudioDriver.Models;

namespace BTAudioDriver.Services;

public interface IBluetoothService : IDisposable
{
    event EventHandler<BluetoothDeviceInfo>? DeviceAdded;

    event EventHandler<BluetoothDeviceInfo>? DeviceRemoved;

    event EventHandler<BluetoothDeviceInfo>? DeviceConnectionChanged;

    Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedAudioDevicesAsync(CancellationToken cancellationToken = default);

    IEnumerable<BluetoothDeviceInfo> GetPairedDevices();

    Task<IReadOnlyList<BluetoothDeviceInfo>> GetConnectedAudioDevicesAsync(CancellationToken cancellationToken = default);

    Task<BluetoothDeviceInfo?> GetDeviceByIdAsync(string deviceId, CancellationToken cancellationToken = default);

    Task StartWatchingAsync(CancellationToken cancellationToken = default);

    void StopWatching();
}
