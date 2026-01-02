using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using BTAudioDriver.Models;
using BTAudioDriver.Native;
using Serilog;

namespace BTAudioDriver.Services;

public class ProfileManager : IProfileManager
{
    private static readonly ILogger Logger = Log.ForContext<ProfileManager>();

    private readonly IAudioEndpointService _audioService;
    private readonly Dictionary<string, DeviceProfileState> _deviceStates = new();
    private bool _disposed;

    public event EventHandler<DeviceProfileState>? ProfileModeChanged;

    public bool RequiresAdminRights => true;

    public bool IsRunningAsAdmin
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    public ProfileManager(IAudioEndpointService audioService)
    {
        _audioService = audioService;
    }

    public async Task<DeviceProfileState?> GetDeviceProfileStateAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var endpoints = _audioService.GetEndpointsForBluetoothDevice(deviceName);
                if (endpoints.Count == 0)
                {
                    Logger.Warning("No audio endpoints found for device: {DeviceName}", deviceName);
                    return null;
                }

                var a2dpEndpoint = endpoints.FirstOrDefault(e => e.IsA2dp);
                var hfpEndpoint = endpoints.FirstOrDefault(e => e.IsHfp);

                var hfpDeviceId = FindHfpDeviceInstanceId(deviceName);

                var isHfpEnabled = hfpEndpoint != null;

                var hasA2dp = a2dpEndpoint != null ||
                    (endpoints.Count > 0 && endpoints.All(e => !e.IsHfp));

                Logger.Debug("Profile state: A2DP={HasA2dp}, HFP={IsHfp}, Endpoints={Count}",
                    hasA2dp, isHfpEnabled, endpoints.Count);

                var state = new DeviceProfileState
                {
                    DeviceId = a2dpEndpoint?.Id ?? hfpEndpoint?.Id ?? endpoints.FirstOrDefault()?.Id ?? "",
                    DeviceName = deviceName,
                    IsA2dpEnabled = hasA2dp,
                    IsHfpEnabled = isHfpEnabled,
                    HfpDeviceInstanceId = hfpDeviceId,
                    A2dpDeviceInstanceId = null,
                    CurrentMode = DetermineCurrentMode(hasA2dp, isHfpEnabled)
                };

                _deviceStates[deviceName] = state;
                return state;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get profile state for {DeviceName}", deviceName);
                return null;
            }
        }, cancellationToken);
    }

    public async Task<bool> SetMusicModeAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        Logger.Information("Setting Music mode for {DeviceName}", deviceName);

        if (!IsRunningAsAdmin)
        {
            Logger.Warning("Admin rights required to change device state");
            return false;
        }

        return await Task.Run(() =>
        {
            try
            {
                var hfpDeviceId = FindHfpDeviceInstanceId(deviceName);
                if (hfpDeviceId == null)
                {
                    Logger.Warning("HFP device not found for {DeviceName}", deviceName);
                    return false;
                }

                var success = DisableDevice(hfpDeviceId);

                if (success)
                {
                    Logger.Information("Successfully disabled HFP for {DeviceName}", deviceName);

                    if (_deviceStates.TryGetValue(deviceName, out var state))
                    {
                        state.IsHfpEnabled = false;
                        state.CurrentMode = ProfileMode.Music;
                        ProfileModeChanged?.Invoke(this, state);
                    }

                    _audioService.Refresh();
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to set Music mode for {DeviceName}", deviceName);
                return false;
            }
        }, cancellationToken);
    }

    public async Task<bool> SetCallsModeAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        Logger.Information("Setting Calls mode for {DeviceName}", deviceName);

        if (!IsRunningAsAdmin)
        {
            Logger.Warning("Admin rights required to change device state");
            return false;
        }

        return await Task.Run(() =>
        {
            try
            {
                var hfpDeviceId = FindHfpDeviceInstanceId(deviceName);
                if (hfpDeviceId == null)
                {
                    Logger.Warning("HFP device not found for {DeviceName}", deviceName);
                    return false;
                }

                var success = EnableDevice(hfpDeviceId);

                if (success)
                {
                    Logger.Information("Successfully enabled HFP for {DeviceName}", deviceName);

                    if (_deviceStates.TryGetValue(deviceName, out var state))
                    {
                        state.IsHfpEnabled = true;
                        state.CurrentMode = ProfileMode.Calls;
                        ProfileModeChanged?.Invoke(this, state);
                    }

                    _audioService.Refresh();
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to set Calls mode for {DeviceName}", deviceName);
                return false;
            }
        }, cancellationToken);
    }

    public async Task<ProfileMode> ToggleModeAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        var state = await GetDeviceProfileStateAsync(deviceName, cancellationToken);
        if (state == null) return ProfileMode.Auto;

        if (state.CurrentMode == ProfileMode.Music)
        {
            await SetCallsModeAsync(deviceName, cancellationToken);
            return ProfileMode.Calls;
        }
        else
        {
            await SetMusicModeAsync(deviceName, cancellationToken);
            return ProfileMode.Music;
        }
    }

    private string? FindHfpDeviceInstanceId(string deviceName)
    {
        var guid = SetupApi.GUID_DEVCLASS_MEDIA;
        using var deviceInfoSet = SetupApi.SetupDiGetClassDevs(
            ref guid,
            null,
            IntPtr.Zero,
            SetupApi.DIGCF_PRESENT);

        if (deviceInfoSet.IsInvalid)
        {
            Logger.Warning("Failed to get device info set");
            return null;
        }

        var deviceInfoData = new SetupApi.SP_DEVINFO_DATA
        {
            cbSize = Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>()
        };

        for (var i = 0; SetupApi.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
        {
            var friendlyName = SetupApi.GetDeviceProperty(deviceInfoSet, ref deviceInfoData, SetupApi.SPDRP_FRIENDLYNAME);
            var description = SetupApi.GetDeviceProperty(deviceInfoSet, ref deviceInfoData, SetupApi.SPDRP_DEVICEDESC);

            var name = friendlyName ?? description ?? "";

            if (name.Contains(deviceName, StringComparison.OrdinalIgnoreCase) &&
                (name.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("Headset", StringComparison.OrdinalIgnoreCase)))
            {
                var instanceId = SetupApi.GetDeviceInstanceId(deviceInfoSet, ref deviceInfoData);
                Logger.Debug("Found HFP device: {Name} ({InstanceId})", name, instanceId);
                return instanceId;
            }
        }

        guid = SetupApi.GUID_DEVCLASS_SOUND;
        using var soundDeviceInfoSet = SetupApi.SetupDiGetClassDevs(
            ref guid,
            null,
            IntPtr.Zero,
            SetupApi.DIGCF_PRESENT);

        if (!soundDeviceInfoSet.IsInvalid)
        {
            deviceInfoData.cbSize = Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>();

            for (var i = 0; SetupApi.SetupDiEnumDeviceInfo(soundDeviceInfoSet, i, ref deviceInfoData); i++)
            {
                var friendlyName = SetupApi.GetDeviceProperty(soundDeviceInfoSet, ref deviceInfoData, SetupApi.SPDRP_FRIENDLYNAME);
                var description = SetupApi.GetDeviceProperty(soundDeviceInfoSet, ref deviceInfoData, SetupApi.SPDRP_DEVICEDESC);

                var name = friendlyName ?? description ?? "";

                if (name.Contains(deviceName, StringComparison.OrdinalIgnoreCase) &&
                    (name.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("Headset", StringComparison.OrdinalIgnoreCase)))
                {
                    var instanceId = SetupApi.GetDeviceInstanceId(soundDeviceInfoSet, ref deviceInfoData);
                    Logger.Debug("Found HFP device in Sound class: {Name} ({InstanceId})", name, instanceId);
                    return instanceId;
                }
            }
        }

        Logger.Debug("HFP device not found for {DeviceName}", deviceName);
        return null;
    }

    private bool DisableDevice(string instanceId)
    {
        return SetDeviceState(instanceId, false);
    }

    private bool EnableDevice(string instanceId)
    {
        return SetDeviceState(instanceId, true);
    }

    private bool SetDeviceState(string instanceId, bool enable)
    {
        Logger.Debug("SetDeviceState: {InstanceId}, enable={Enable}", instanceId, enable);

        var classGuids = new[] { SetupApi.GUID_DEVCLASS_MEDIA, SetupApi.GUID_DEVCLASS_SOUND };

        foreach (var classGuid in classGuids)
        {
            var guid = classGuid;
            using var deviceInfoSet = SetupApi.SetupDiGetClassDevs(
                ref guid,
                null,
                IntPtr.Zero,
                SetupApi.DIGCF_PRESENT);

            if (deviceInfoSet.IsInvalid)
                continue;

            var deviceInfoData = new SetupApi.SP_DEVINFO_DATA
            {
                cbSize = Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>()
            };

            for (var i = 0; SetupApi.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                var currentInstanceId = SetupApi.GetDeviceInstanceId(deviceInfoSet, ref deviceInfoData);
                if (currentInstanceId == null)
                    continue;

                if (currentInstanceId.Equals(instanceId, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Debug("Found device in class {ClassGuid}", classGuid);

                    var result = SetupApi.SetDeviceEnabled(deviceInfoSet, ref deviceInfoData, enable);

                    if (!result)
                    {
                        var error = Marshal.GetLastWin32Error();
                        Logger.Warning("Failed to {Action} device {InstanceId}: {Error}",
                            enable ? "enable" : "disable", instanceId, new Win32Exception(error).Message);
                    }
                    else
                    {
                        Logger.Information("Successfully {Action} device {InstanceId}",
                            enable ? "enabled" : "disabled", instanceId);
                    }

                    return result;
                }
            }
        }

        Logger.Warning("Device not found by Instance ID: {InstanceId}", instanceId);
        return false;
    }

    private static ProfileMode DetermineCurrentMode(bool hasA2dp, bool hasHfp)
    {
        if (hasHfp) return ProfileMode.Calls;
        if (hasA2dp) return ProfileMode.Music;
        return ProfileMode.Auto;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _deviceStates.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
