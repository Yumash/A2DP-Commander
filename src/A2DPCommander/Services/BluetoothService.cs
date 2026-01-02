using System.Collections.Concurrent;
using BTAudioDriver.Models;
using Serilog;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BTAudioDriver.Services;

public class BluetoothService : IBluetoothService
{
    private static readonly ILogger Logger = Log.ForContext<BluetoothService>();

    private static readonly Guid A2dpSinkServiceClass = new("0000110b-0000-1000-8000-00805f9b34fb");
    private static readonly Guid HfpServiceClass = new("0000111e-0000-1000-8000-00805f9b34fb");
    private static readonly Guid AvrcpServiceClass = new("0000110e-0000-1000-8000-00805f9b34fb");

    private readonly ConcurrentDictionary<string, BluetoothDeviceInfo> _devices = new();
    private DeviceWatcher? _deviceWatcher;
    private bool _isWatching;
    private bool _disposed;

    public event EventHandler<BluetoothDeviceInfo>? DeviceAdded;
    public event EventHandler<BluetoothDeviceInfo>? DeviceRemoved;
    public event EventHandler<BluetoothDeviceInfo>? DeviceConnectionChanged;

    public async Task<IReadOnlyList<BluetoothDeviceInfo>> GetPairedAudioDevicesAsync(CancellationToken cancellationToken = default)
    {
        var devices = new List<BluetoothDeviceInfo>();

        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var deviceInfos = await DeviceInformation.FindAllAsync(selector).AsTask(cancellationToken);

        foreach (var deviceInfo in deviceInfos)
        {
            try
            {
                var btDevice = await BluetoothDevice.FromIdAsync(deviceInfo.Id).AsTask(cancellationToken);
                if (btDevice == null) continue;

                var info = await CreateDeviceInfoAsync(btDevice, cancellationToken);
                if (info.IsAudioDevice)
                {
                    devices.Add(info);
                    _devices.TryAdd(info.Id, info);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to get device info for {DeviceId}", deviceInfo.Id);
            }
        }

        Logger.Information("Found {Count} paired audio devices", devices.Count);
        return devices;
    }

    public async Task<IReadOnlyList<BluetoothDeviceInfo>> GetConnectedAudioDevicesAsync(CancellationToken cancellationToken = default)
    {
        var allDevices = await GetPairedAudioDevicesAsync(cancellationToken);
        var connectedDevices = allDevices.Where(d => d.IsConnected).ToList();

        Logger.Information("Found {Count} connected audio devices", connectedDevices.Count);
        return connectedDevices;
    }

    public IEnumerable<BluetoothDeviceInfo> GetPairedDevices()
    {
        return _devices.Values.ToList();
    }

    public async Task<BluetoothDeviceInfo?> GetDeviceByIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (_devices.TryGetValue(deviceId, out var cachedDevice))
        {
            try
            {
                var btDevice = await BluetoothDevice.FromIdAsync(deviceId).AsTask(cancellationToken);
                if (btDevice != null)
                {
                    cachedDevice.IsConnected = btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to update connection status for {DeviceId}", deviceId);
            }
            return cachedDevice;
        }

        try
        {
            var btDevice = await BluetoothDevice.FromIdAsync(deviceId).AsTask(cancellationToken);
            if (btDevice == null) return null;

            var info = await CreateDeviceInfoAsync(btDevice, cancellationToken);
            _devices.TryAdd(info.Id, info);
            return info;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to get device by ID: {DeviceId}", deviceId);
            return null;
        }
    }

    public Task StartWatchingAsync(CancellationToken cancellationToken = default)
    {
        if (_isWatching) return Task.CompletedTask;

        string[] requestedProperties =
        {
            "System.Devices.Aep.IsConnected",
            "System.Devices.Aep.IsPaired",
            "System.Devices.Aep.DeviceAddress"
        };

        var selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);

        _deviceWatcher = DeviceInformation.CreateWatcher(
            selector,
            requestedProperties,
            DeviceInformationKind.AssociationEndpoint);

        _deviceWatcher.Added += OnDeviceAdded;
        _deviceWatcher.Removed += OnDeviceRemoved;
        _deviceWatcher.Updated += OnDeviceUpdated;
        _deviceWatcher.EnumerationCompleted += OnEnumerationCompleted;
        _deviceWatcher.Stopped += OnWatcherStopped;

        _deviceWatcher.Start();
        _isWatching = true;

        Logger.Information("Started watching for Bluetooth devices");
        return Task.CompletedTask;
    }

    public void StopWatching()
    {
        if (!_isWatching || _deviceWatcher == null) return;

        if (_deviceWatcher.Status == DeviceWatcherStatus.Started ||
            _deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
        {
            _deviceWatcher.Stop();
        }

        _deviceWatcher.Added -= OnDeviceAdded;
        _deviceWatcher.Removed -= OnDeviceRemoved;
        _deviceWatcher.Updated -= OnDeviceUpdated;
        _deviceWatcher.EnumerationCompleted -= OnEnumerationCompleted;
        _deviceWatcher.Stopped -= OnWatcherStopped;

        _deviceWatcher = null;
        _isWatching = false;

        Logger.Information("Stopped watching for Bluetooth devices");
    }

    private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
    {
        try
        {
            var btDevice = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
            if (btDevice == null) return;

            var info = await CreateDeviceInfoAsync(btDevice);
            if (!info.IsAudioDevice) return;

            _devices.TryAdd(info.Id, info);
            Logger.Information("Device added: {DeviceName} ({Mac})", info.Name, info.MacAddress);
            DeviceAdded?.Invoke(this, info);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error processing added device: {DeviceId}", deviceInfo.Id);
        }
    }

    private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
    {
        if (_devices.TryRemove(deviceInfoUpdate.Id, out var removedDevice))
        {
            Logger.Information("Device removed: {DeviceName}", removedDevice.Name);
            DeviceRemoved?.Invoke(this, removedDevice);
        }
    }

    private async void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
    {
        if (!_devices.TryGetValue(deviceInfoUpdate.Id, out var existingDevice)) return;

        try
        {
            var btDevice = await BluetoothDevice.FromIdAsync(deviceInfoUpdate.Id);
            if (btDevice == null) return;

            var wasConnected = existingDevice.IsConnected;
            existingDevice.IsConnected = btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;

            if (wasConnected != existingDevice.IsConnected)
            {
                Logger.Information("Device {DeviceName} connection changed: {Status}",
                    existingDevice.Name,
                    existingDevice.IsConnected ? "Connected" : "Disconnected");
                DeviceConnectionChanged?.Invoke(this, existingDevice);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error updating device: {DeviceId}", deviceInfoUpdate.Id);
        }
    }

    private void OnEnumerationCompleted(DeviceWatcher sender, object args)
    {
        Logger.Debug("Device enumeration completed. Found {Count} devices", _devices.Count);
    }

    private void OnWatcherStopped(DeviceWatcher sender, object args)
    {
        Logger.Debug("Device watcher stopped");
    }

    private async Task<BluetoothDeviceInfo> CreateDeviceInfoAsync(BluetoothDevice btDevice, CancellationToken cancellationToken = default)
    {
        var supportsA2dp = false;
        var supportsHfp = false;
        var supportsAvrcp = false;

        try
        {
            var rfcommServices = await btDevice.GetRfcommServicesAsync().AsTask(cancellationToken);

            foreach (var service in rfcommServices.Services)
            {
                var serviceId = service.ServiceId.Uuid;

                if (serviceId == A2dpSinkServiceClass) supportsA2dp = true;
                else if (serviceId == HfpServiceClass) supportsHfp = true;
                else if (serviceId == AvrcpServiceClass) supportsAvrcp = true;
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Could not get RFCOMM services for {DeviceName}, assuming audio device", btDevice.Name);
            supportsA2dp = true;
            supportsHfp = true;
        }

        return new BluetoothDeviceInfo
        {
            Id = btDevice.DeviceId,
            Name = btDevice.Name,
            BluetoothAddress = btDevice.BluetoothAddress,
            IsConnected = btDevice.ConnectionStatus == BluetoothConnectionStatus.Connected,
            IsPaired = btDevice.DeviceInformation.Pairing.IsPaired,
            SupportsA2dp = supportsA2dp,
            SupportsHfp = supportsHfp,
            SupportsAvrcp = supportsAvrcp
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopWatching();
        _devices.Clear();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
