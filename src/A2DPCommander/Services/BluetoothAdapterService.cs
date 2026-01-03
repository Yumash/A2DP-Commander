using System.Runtime.InteropServices;
using BTAudioDriver.Models;
using BTAudioDriver.Native;
using Microsoft.Win32;
using Serilog;

namespace BTAudioDriver.Services;

public class BluetoothAdapterService : IBluetoothAdapterService
{
    private static readonly ILogger Logger = Log.ForContext<BluetoothAdapterService>();

    public event EventHandler<BluetoothAdapterInfo>? AdapterChanged;

    public List<BluetoothAdapterInfo> GetAllAdapters()
    {
        var adapters = new List<BluetoothAdapterInfo>();

        try
        {
            var guid = SetupApi.GUID_DEVCLASS_BLUETOOTH;
            using var deviceInfoSet = SetupApi.SetupDiGetClassDevs(
                ref guid,
                null,
                IntPtr.Zero,
                SetupApi.DIGCF_PRESENT);

            if (deviceInfoSet.IsInvalid)
            {
                Logger.Warning("Failed to get Bluetooth device info set");
                return adapters;
            }

            var deviceInfoData = new SetupApi.SP_DEVINFO_DATA
            {
                cbSize = Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>()
            };

            for (var i = 0; SetupApi.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                var friendlyName = SetupApi.GetDeviceProperty(deviceInfoSet, ref deviceInfoData, SetupApi.SPDRP_FRIENDLYNAME);
                var description = SetupApi.GetDeviceProperty(deviceInfoSet, ref deviceInfoData, SetupApi.SPDRP_DEVICEDESC);
                var instanceId = SetupApi.GetDeviceInstanceId(deviceInfoSet, ref deviceInfoData);

                var name = friendlyName ?? description ?? "Unknown Bluetooth Adapter";

                if (!IsBluetoothRadio(name, instanceId))
                    continue;

                var adapter = new BluetoothAdapterInfo
                {
                    Name = name,
                    DeviceInstanceId = instanceId ?? "",
                    IsEnabled = IsDeviceEnabled(instanceId),
                    IsActive = IsAdapterActive(instanceId)
                };

                DetectManufacturer(adapter);
                DetectSupportedCodecs(adapter);

                adapters.Add(adapter);
                Logger.Debug("Found Bluetooth adapter: {Name} ({InstanceId}), Enabled={Enabled}, Codecs={Codecs}",
                    adapter.Name, adapter.DeviceInstanceId, adapter.IsEnabled, adapter.SupportedCodecsDisplay);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to enumerate Bluetooth adapters");
        }

        return adapters;
    }

    public BluetoothAdapterInfo? GetActiveAdapter()
    {
        return GetAllAdapters().FirstOrDefault(a => a.IsEnabled && a.IsActive);
    }

    public bool SetActiveAdapter(string deviceInstanceId)
    {
        try
        {
            var adapters = GetAllAdapters();
            var targetAdapter = adapters.FirstOrDefault(a =>
                a.DeviceInstanceId.Equals(deviceInstanceId, StringComparison.OrdinalIgnoreCase));

            if (targetAdapter == null)
            {
                Logger.Warning("Target adapter not found: {InstanceId}", deviceInstanceId);
                return false;
            }

            foreach (var adapter in adapters.Where(a => a.IsEnabled &&
                !a.DeviceInstanceId.Equals(deviceInstanceId, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.Information("Disabling adapter: {Name}", adapter.Name);
                if (!DisableAdapter(adapter.DeviceInstanceId))
                {
                    Logger.Warning("Failed to disable adapter: {Name}", adapter.Name);
                }
            }

            if (!targetAdapter.IsEnabled)
            {
                Logger.Information("Enabling adapter: {Name}", targetAdapter.Name);
                if (!EnableAdapter(deviceInstanceId))
                {
                    Logger.Error("Failed to enable target adapter: {Name}", targetAdapter.Name);
                    return false;
                }
            }

            AdapterChanged?.Invoke(this, targetAdapter);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set active adapter");
            return false;
        }
    }

    public bool EnableAdapter(string deviceInstanceId)
    {
        return SetAdapterState(deviceInstanceId, true);
    }

    public bool DisableAdapter(string deviceInstanceId)
    {
        return SetAdapterState(deviceInstanceId, false);
    }

    private bool SetAdapterState(string deviceInstanceId, bool enable)
    {
        try
        {
            var guid = SetupApi.GUID_DEVCLASS_BLUETOOTH;
            using var deviceInfoSet = SetupApi.SetupDiGetClassDevs(
                ref guid,
                null,
                IntPtr.Zero,
                SetupApi.DIGCF_PRESENT);

            if (deviceInfoSet.IsInvalid)
                return false;

            var deviceInfoData = new SetupApi.SP_DEVINFO_DATA
            {
                cbSize = Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>()
            };

            for (var i = 0; SetupApi.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                var instanceId = SetupApi.GetDeviceInstanceId(deviceInfoSet, ref deviceInfoData);
                if (instanceId?.Equals(deviceInstanceId, StringComparison.OrdinalIgnoreCase) == true)
                {
                    var result = SetupApi.SetDeviceEnabled(deviceInfoSet, ref deviceInfoData, enable);
                    Logger.Information("{Action} adapter {InstanceId}: {Result}",
                        enable ? "Enabled" : "Disabled", deviceInstanceId, result ? "Success" : "Failed");
                    return result;
                }
            }

            Logger.Warning("Adapter not found: {InstanceId}", deviceInstanceId);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to {Action} adapter {InstanceId}", enable ? "enable" : "disable", deviceInstanceId);
            return false;
        }
    }

    private static bool IsBluetoothRadio(string name, string? instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return false;

        var lowerInstanceId = instanceId.ToLowerInvariant();
        var lowerName = name.ToLowerInvariant();

        if (lowerInstanceId.Contains("bthenum") ||
            lowerInstanceId.Contains("bth\\ms") ||
            lowerName.Contains("enumerator") ||
            lowerName.Contains("rfcomm") ||
            lowerName.Contains("protocol") ||
            lowerName.Contains("перечислитель") ||
            lowerName.Contains("microsoft bluetooth"))
            return false;

        if (lowerInstanceId.StartsWith("usb\\") &&
            (lowerInstanceId.Contains("vid_8087") ||
             lowerInstanceId.Contains("vid_0bda") ||
             lowerInstanceId.Contains("vid_0a5c") ||
             lowerInstanceId.Contains("vid_0cf3") ||
             lowerInstanceId.Contains("vid_13d3") ||
             lowerInstanceId.Contains("vid_2b89") ||
             lowerInstanceId.Contains("vid_0a12")))
            return true;

        if (lowerInstanceId.StartsWith("usb\\") &&
            lowerName.Contains("bluetooth") &&
            (lowerName.Contains("radio") || lowerName.Contains("adapter") || lowerName.Contains("wireless")))
            return true;

        return false;
    }

    private static bool IsDeviceEnabled(string? instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return false;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Enum\{instanceId}");
            if (key == null) return true;

            var configFlags = key.GetValue("ConfigFlags");
            if (configFlags is int flags)
            {
                return (flags & 0x00000001) == 0;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }

    private static bool IsAdapterActive(string? instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return false;

        try
        {
            var guid = SetupApi.GUID_DEVCLASS_BLUETOOTH;
            using var deviceInfoSet = SetupApi.SetupDiGetClassDevs(
                ref guid,
                null,
                IntPtr.Zero,
                SetupApi.DIGCF_PRESENT);

            if (deviceInfoSet.IsInvalid)
                return false;

            var deviceInfoData = new SetupApi.SP_DEVINFO_DATA
            {
                cbSize = Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>()
            };

            for (var i = 0; SetupApi.SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
            {
                var currentInstanceId = SetupApi.GetDeviceInstanceId(deviceInfoSet, ref deviceInfoData);
                if (currentInstanceId?.Equals(instanceId, StringComparison.OrdinalIgnoreCase) == true)
                {
                    var status = SetupApi.GetDeviceStatus(deviceInfoSet, ref deviceInfoData);
                    return (status & SetupApi.DN_STARTED) != 0;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void DetectManufacturer(BluetoothAdapterInfo adapter)
    {
        var name = adapter.Name.ToLowerInvariant();
        var instanceId = adapter.DeviceInstanceId.ToLowerInvariant();

        adapter.IsIntel = name.Contains("intel") || instanceId.Contains("vid_8087");
        adapter.IsRealtek = name.Contains("realtek") || instanceId.Contains("vid_0bda") || instanceId.Contains("vid_2b89");

        if (adapter.IsIntel)
            adapter.Manufacturer = "Intel";
        else if (instanceId.Contains("vid_2b89"))
            adapter.Manufacturer = "UGREEN (Realtek)";
        else if (adapter.IsRealtek)
            adapter.Manufacturer = "Realtek";
        else if (instanceId.Contains("vid_0a5c"))
            adapter.Manufacturer = "Broadcom";
        else if (instanceId.Contains("vid_0cf3"))
            adapter.Manufacturer = "Qualcomm/Atheros";
        else if (instanceId.Contains("vid_13d3"))
            adapter.Manufacturer = "IMC Networks";
        else if (instanceId.Contains("vid_0a12"))
            adapter.Manufacturer = "Cambridge Silicon Radio";
    }

    private static void DetectSupportedCodecs(BluetoothAdapterInfo adapter)
    {
        adapter.SupportsAAC = CheckAACSupport();

        var instanceId = adapter.DeviceInstanceId.ToLowerInvariant();

        if (adapter.IsIntel)
        {
            adapter.SupportsAptX = false;
            adapter.SupportsAptXHD = false;
            adapter.SupportsLDAC = false;
        }
        else if (adapter.IsRealtek || instanceId.Contains("vid_2b89"))
        {
            adapter.SupportsAptX = true;
            adapter.SupportsAptXHD = false;
            adapter.SupportsLDAC = false;
        }
        else if (instanceId.Contains("vid_0a12"))
        {
            adapter.SupportsAptX = true;
            adapter.SupportsAptXHD = true;
            adapter.SupportsLDAC = false;
        }
        else
        {
            adapter.SupportsAptX = CheckAptXSupport();
            adapter.SupportsAptXHD = CheckAptXHDSupport();
            adapter.SupportsLDAC = CheckLDACSupport();
        }
    }

    private static bool CheckAACSupport()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BthA2dp\Parameters");
            if (key != null)
            {
                var aacEnabled = key.GetValue("BluetoothAacEnable");
                if (aacEnabled is int value)
                    return value != 0;
            }
            return true;
        }
        catch
        {
            return true;
        }
    }

    private static bool CheckAptXSupport()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BthA2dp\Parameters");
            if (key != null)
            {
                var aptxEnabled = key.GetValue("BluetoothAptXEnable");
                if (aptxEnabled is int value && value != 0)
                    return true;
            }

            using var aptxKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Qualcomm\aptX");
            if (aptxKey != null)
                return true;

            using var codecKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render");
            if (codecKey != null)
            {
                foreach (var subKeyName in codecKey.GetSubKeyNames())
                {
                    using var deviceKey = codecKey.OpenSubKey(subKeyName + @"\Properties");
                    if (deviceKey != null)
                    {
                        foreach (var valueName in deviceKey.GetValueNames())
                        {
                            var val = deviceKey.GetValue(valueName)?.ToString()?.ToLowerInvariant() ?? "";
                            if (val.Contains("aptx"))
                                return true;
                        }
                    }
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckAptXHDSupport()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Qualcomm\aptXHD");
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool CheckLDACSupport()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Sony\LDAC");
            if (key != null)
                return true;

            using var key2 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BthA2dp\Parameters");
            if (key2 != null)
            {
                var ldacEnabled = key2.GetValue("BluetoothLDACEnable");
                if (ldacEnabled is int value && value != 0)
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
