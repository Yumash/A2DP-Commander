using System.Runtime.InteropServices;
using BTAudioDriver.Models;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using Serilog;

namespace BTAudioDriver.Services;

public class AudioEndpointService : IAudioEndpointService, IMMNotificationClient
{
    private static readonly ILogger Logger = Log.ForContext<AudioEndpointService>();

    private readonly MMDeviceEnumerator _enumerator;
    private readonly List<AudioEndpointInfo> _playbackEndpoints = new();
    private readonly List<AudioEndpointInfo> _recordingEndpoints = new();
    private readonly object _lock = new();
    private bool _disposed;

    public event EventHandler<AudioEndpointInfo>? EndpointAdded;
    public event EventHandler<AudioEndpointInfo>? EndpointRemoved;
    public event EventHandler<AudioEndpointInfo>? DefaultDeviceChanged;

    public AudioEndpointService()
    {
        _enumerator = new MMDeviceEnumerator();
        _enumerator.RegisterEndpointNotificationCallback(this);
        Refresh();
    }

    public IReadOnlyList<AudioEndpointInfo> GetPlaybackEndpoints()
    {
        lock (_lock)
        {
            return _playbackEndpoints.ToList();
        }
    }

    public IReadOnlyList<AudioEndpointInfo> GetRecordingEndpoints()
    {
        lock (_lock)
        {
            return _recordingEndpoints.ToList();
        }
    }

    public IReadOnlyList<AudioEndpointInfo> GetBluetoothEndpoints()
    {
        lock (_lock)
        {
            return _playbackEndpoints
                .Concat(_recordingEndpoints)
                .Where(e => e.IsBluetooth)
                .Distinct()
                .ToList();
        }
    }

    public IReadOnlyList<AudioEndpointInfo> GetEndpointsForBluetoothDevice(string deviceName)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                return new List<AudioEndpointInfo>();
            }

            var allEndpoints = _playbackEndpoints.Concat(_recordingEndpoints).ToList();

            var btEndpoints = allEndpoints.Where(e => e.IsBluetooth).ToList();

            if (btEndpoints.Count == 0)
            {
                var byName = allEndpoints
                    .Where(e => e.FriendlyName.Contains(deviceName, StringComparison.OrdinalIgnoreCase) ||
                                e.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (byName.Count > 0)
                {
                    Logger.Information("IsBluetooth missed {Count} endpoints for '{DeviceName}', found by name fallback",
                        byName.Count, deviceName);
                    return byName;
                }
            }

            var result = btEndpoints
                .Where(e => e.FriendlyName.Contains(deviceName, StringComparison.OrdinalIgnoreCase) ||
                            e.Name.Contains(deviceName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (result.Count == 0 && btEndpoints.Count > 0)
            {
                Logger.Warning("Device '{DeviceName}' not matched. Available BT endpoints: {Endpoints}",
                    deviceName,
                    string.Join(", ", btEndpoints.Select(e => $"'{e.FriendlyName}'")));
            }

            return result;
        }
    }

    public AudioEndpointInfo? GetDefaultPlaybackDevice()
    {
        try
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return CreateEndpointInfo(device, true);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to get default playback device");
            return null;
        }
    }

    public AudioEndpointInfo? GetDefaultRecordingDevice()
    {
        try
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            return CreateEndpointInfo(device, false);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to get default recording device");
            return null;
        }
    }

    public bool SetDefaultPlaybackDevice(string deviceId)
    {
        try
        {
            var policyConfig = new PolicyConfigClient();
            policyConfig.SetDefaultEndpoint(deviceId, Role.Multimedia);
            policyConfig.SetDefaultEndpoint(deviceId, Role.Console);

            Logger.Information("Set default playback device: {DeviceId}", deviceId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set default playback device: {DeviceId}", deviceId);
            return false;
        }
    }

    public bool SetDefaultRecordingDevice(string deviceId)
    {
        try
        {
            var policyConfig = new PolicyConfigClient();
            policyConfig.SetDefaultEndpoint(deviceId, Role.Communications);

            Logger.Information("Set default recording device: {DeviceId}", deviceId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set default recording device: {DeviceId}", deviceId);
            return false;
        }
    }

    public void Refresh()
    {
        lock (_lock)
        {
            _playbackEndpoints.Clear();
            _recordingEndpoints.Clear();

            var playbackDevices = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var device in playbackDevices)
            {
                try
                {
                    var info = CreateEndpointInfo(device, true);
                    _playbackEndpoints.Add(info);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to get endpoint info for {DeviceId}", device.ID);
                }
            }

            var recordingDevices = _enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            foreach (var device in recordingDevices)
            {
                try
                {
                    var info = CreateEndpointInfo(device, false);
                    _recordingEndpoints.Add(info);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to get endpoint info for {DeviceId}", device.ID);
                }
            }

            var btCount = _playbackEndpoints.Count(e => e.IsBluetooth);
            Logger.Information("Refreshed audio endpoints: {Playback} playback ({BtCount} BT), {Recording} recording",
                _playbackEndpoints.Count, btCount, _recordingEndpoints.Count);

            if (_playbackEndpoints.Count > 0)
            {
                var names = _playbackEndpoints.Select(e => $"{(e.IsBluetooth ? "[BT]" : "")} {e.FriendlyName}");
                Logger.Information("Playback devices: {Devices}", string.Join("; ", names));
            }
        }
    }

    private AudioEndpointInfo CreateEndpointInfo(MMDevice device, bool isPlayback)
    {
        var friendlyName = device.FriendlyName;
        var deviceName = device.DeviceFriendlyName;

        var isBluetooth = IsBluetooth(device);
        var profile = BluetoothAudioProfile.Unknown;

        if (isBluetooth)
        {
            profile = DetermineBluetoothProfile(friendlyName);
        }

        var isDefault = false;
        try
        {
            using var defaultDevice = _enumerator.GetDefaultAudioEndpoint(
                isPlayback ? DataFlow.Render : DataFlow.Capture,
                Role.Multimedia);
            isDefault = device.ID == defaultDevice.ID;
        }
        catch
        {
        }

        return new AudioEndpointInfo
        {
            Id = device.ID,
            Name = deviceName,
            FriendlyName = friendlyName,
            IsPlayback = isPlayback,
            IsRecording = !isPlayback,
            IsActive = device.State == DeviceState.Active,
            IsDefault = isDefault,
            IsBluetooth = isBluetooth,
            BluetoothProfile = profile
        };
    }

    private static bool IsBluetooth(MMDevice device)
    {
        try
        {
            var enumeratorKey = new PropertyKey(new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), 24);
            if (device.Properties.Contains(enumeratorKey))
            {
                var enumerator = device.Properties[enumeratorKey].Value?.ToString();
                if (enumerator?.Contains("BTH", StringComparison.OrdinalIgnoreCase) == true ||
                    enumerator?.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }
            }

            var descKey = new PropertyKey(new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), 2);
            if (device.Properties.Contains(descKey))
            {
                var desc = device.Properties[descKey].Value?.ToString();
                if (desc?.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }

            var deviceId = device.ID;
            if (deviceId.Contains("BTHENUM", StringComparison.OrdinalIgnoreCase) ||
                deviceId.Contains("BTHHFENUM", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var name = device.FriendlyName;
            return name.Contains("Stereo", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Headset", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Headphones", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Наушники", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Гарнитура", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static BluetoothAudioProfile DetermineBluetoothProfile(string friendlyName)
    {
        if (friendlyName.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase) ||
            friendlyName.Contains("Headset", StringComparison.OrdinalIgnoreCase) ||
            friendlyName.Contains("Гарнитура", StringComparison.OrdinalIgnoreCase))
            return BluetoothAudioProfile.Hfp;

        if (friendlyName.Contains("Stereo", StringComparison.OrdinalIgnoreCase) ||
            friendlyName.Contains("Headphones", StringComparison.OrdinalIgnoreCase) ||
            friendlyName.Contains("Наушники", StringComparison.OrdinalIgnoreCase) ||
            friendlyName.Contains("Speaker", StringComparison.OrdinalIgnoreCase) ||
            friendlyName.Contains("Динамик", StringComparison.OrdinalIgnoreCase))
            return BluetoothAudioProfile.A2dp;

        return BluetoothAudioProfile.Unknown;
    }

    #region IMMNotificationClient

    public void OnDeviceStateChanged(string deviceId, DeviceState newState)
    {
        Logger.Debug("Device state changed: {DeviceId} -> {State}", deviceId, newState);
        Refresh();
    }

    public void OnDeviceAdded(string pwstrDeviceId)
    {
        Logger.Debug("Device added: {DeviceId}", pwstrDeviceId);
        Refresh();

        try
        {
            using var device = _enumerator.GetDevice(pwstrDeviceId);
            var info = CreateEndpointInfo(device, device.DataFlow == DataFlow.Render);
            EndpointAdded?.Invoke(this, info);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to get info for added device: {DeviceId}", pwstrDeviceId);
        }
    }

    public void OnDeviceRemoved(string deviceId)
    {
        Logger.Debug("Device removed: {DeviceId}", deviceId);

        AudioEndpointInfo? removedInfo;
        lock (_lock)
        {
            removedInfo = _playbackEndpoints.FirstOrDefault(e => e.Id == deviceId)
                       ?? _recordingEndpoints.FirstOrDefault(e => e.Id == deviceId);
        }

        Refresh();

        if (removedInfo != null)
        {
            EndpointRemoved?.Invoke(this, removedInfo);
        }
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        Logger.Debug("Default device changed: {Flow}/{Role} -> {DeviceId}", flow, role, defaultDeviceId);

        if (role == Role.Multimedia)
        {
            try
            {
                using var device = _enumerator.GetDevice(defaultDeviceId);
                var info = CreateEndpointInfo(device, flow == DataFlow.Render);
                DefaultDeviceChanged?.Invoke(this, info);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to get info for default device: {DeviceId}", defaultDeviceId);
            }
        }

        Refresh();
    }

    public void OnPropertyValueChanged(string pwstrDeviceId, PropertyKey key)
    {
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;

        _enumerator.UnregisterEndpointNotificationCallback(this);
        _enumerator.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

#region PolicyConfig COM Interface

[ComImport]
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPolicyConfig
{
    void GetMixFormat(string pszDeviceName, out IntPtr ppFormat);
    void GetDeviceFormat(string pszDeviceName, bool bDefault, out IntPtr ppFormat);
    void ResetDeviceFormat(string pszDeviceName);
    void SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr mixFormat);
    void GetProcessingPeriod(string pszDeviceName, bool bDefault, out long pmftDefaultPeriod, out long pmftMinimumPeriod);
    void SetProcessingPeriod(string pszDeviceName, long pmftPeriod);
    void GetShareMode(string pszDeviceName, out IntPtr pMode);
    void SetShareMode(string pszDeviceName, IntPtr mode);
    void GetPropertyValue(string pszDeviceName, bool bFxStore, ref PropertyKey key, out PropVariant pv);
    void SetPropertyValue(string pszDeviceName, bool bFxStore, ref PropertyKey key, ref PropVariant pv);
    void SetDefaultEndpoint(string pszDeviceName, Role eRole);
    void SetEndpointVisibility(string pszDeviceName, bool bVisible);
}

internal class PolicyConfigClient
{
    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(
        ref Guid rclsid,
        IntPtr pUnkOuter,
        uint dwClsContext,
        ref Guid riid,
        out IntPtr ppv);

    private readonly IPolicyConfig _policyConfig;

    public PolicyConfigClient()
    {
        var clsid = new Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");
        var iid = new Guid("F8679F50-850A-41CF-9C72-430F290290C8");

        var hr = CoCreateInstance(ref clsid, IntPtr.Zero, 1, ref iid, out var ptr);
        if (hr != 0)
            throw new COMException("Failed to create PolicyConfig instance", hr);

        _policyConfig = (IPolicyConfig)Marshal.GetObjectForIUnknown(ptr);
        Marshal.Release(ptr);
    }

    public void SetDefaultEndpoint(string deviceId, Role role)
    {
        _policyConfig.SetDefaultEndpoint(deviceId, role);
    }
}

#endregion
