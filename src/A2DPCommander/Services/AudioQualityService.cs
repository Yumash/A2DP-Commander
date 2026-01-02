using System.Runtime.InteropServices;
using BTAudioDriver.Models;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using Serilog;

namespace BTAudioDriver.Services;

public class AudioQualityService : IAudioQualityService
{
    private static readonly ILogger Logger = Log.ForContext<AudioQualityService>();

    private readonly IAudioEndpointService _audioEndpointService;

    private const string BthAudioRegPath = @"SYSTEM\CurrentControlSet\Services\BthA2dp\Parameters";
    private const string AudioEnhancementsRegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio";

    public AudioQualityService(IAudioEndpointService audioEndpointService)
    {
        _audioEndpointService = audioEndpointService;
    }

    public AudioQualityInfo? GetCurrentQualityInfo(string deviceName)
    {
        try
        {
            var endpoint = FindBluetoothEndpoint(deviceName);
            if (endpoint == null)
            {
                Logger.Warning("Bluetooth endpoint not found for {Device}", deviceName);
                return null;
            }

            var info = new AudioQualityInfo
            {
                CurrentCodec = DetectCurrentCodec(deviceName),
                CodecName = GetCurrentCodecName(deviceName),
                SupportedCodecs = GetSupportedCodecs(deviceName)
            };

            try
            {
                using var device = endpoint;
                var format = device.AudioClient.MixFormat;

                info.SampleRate = format.SampleRate;
                info.BitDepth = format.BitsPerSample;
                info.Channels = format.Channels;

                info.Bitrate = EstimateBitrate(info.CurrentCodec, info.SampleRate);

                info.EnhancementsEnabled = AreEnhancementsEnabled(device.ID);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to get audio format details");
                info.SampleRate = 48000;
                info.BitDepth = 16;
                info.Channels = 2;
                info.Bitrate = 328;
            }

            return info;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to get audio quality info for {Device}", deviceName);
            return null;
        }
    }

    public List<BluetoothCodec> GetSupportedCodecs(string deviceName)
    {
        var codecs = new List<BluetoothCodec> { BluetoothCodec.SBC };

        try
        {
            if (IsAACSupported())
            {
                codecs.Add(BluetoothCodec.AAC);
            }

            if (IsLDACSupported())
            {
                codecs.Add(BluetoothCodec.LDAC);
            }

            if (IsAptXSupported())
            {
                codecs.Add(BluetoothCodec.AptX);

                if (IsAptXHDSupported())
                {
                    codecs.Add(BluetoothCodec.AptXHD);
                }
            }

        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to detect supported codecs");
        }

        return codecs;
    }

    public async Task<bool> ApplyQualitySettingsAsync(string deviceName, AudioQualitySettings settings)
    {
        try
        {
            Logger.Information("Applying audio quality settings for {Device}: Codec={Codec}, SampleRate={SampleRate}",
                deviceName, settings.PreferredCodec, settings.PreferredSampleRate);

            var endpoint = FindBluetoothEndpoint(deviceName);
            if (endpoint == null)
            {
                Logger.Warning("Bluetooth endpoint not found");
                return false;
            }

            var deviceId = endpoint.ID;

            if (settings.DisableEnhancements)
            {
                await DisableEnhancementsAsync(deviceId);
            }
            else
            {
                await EnableEnhancementsAsync(deviceId);
            }

            if (settings.SetAsDefaultDevice)
            {
                await SetDefaultDeviceAsync(deviceId);
            }

            if (settings.PreferredCodec != BluetoothCodec.Auto)
            {
                SetPreferredCodecInRegistry(settings.PreferredCodec);
            }

            Logger.Information("Audio quality settings applied successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to apply audio quality settings");
            return false;
        }
    }

    public async Task<bool> DisableEnhancementsAsync(string deviceId)
    {
        try
        {
            Logger.Information("Disabling audio enhancements for device {DeviceId}", deviceId);

            await Task.Run(() =>
            {
                try
                {
                    SetEnhancementsEnabled(deviceId, false);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to disable enhancements via registry");
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to disable audio enhancements");
            return false;
        }
    }

    public async Task<bool> EnableEnhancementsAsync(string deviceId)
    {
        try
        {
            Logger.Information("Enabling audio enhancements for device {DeviceId}", deviceId);

            await Task.Run(() =>
            {
                SetEnhancementsEnabled(deviceId, true);
            });

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to enable audio enhancements");
            return false;
        }
    }

    public async Task<bool> SetDefaultDeviceAsync(string deviceId)
    {
        try
        {
            Logger.Information("Setting default audio device: {DeviceId}", deviceId);

            await Task.Run(() =>
            {
                try
                {
                    var policyConfig = new PolicyConfigClient();
                    policyConfig.SetDefaultEndpoint(deviceId, Role.Multimedia);
                    policyConfig.SetDefaultEndpoint(deviceId, Role.Console);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to set default device via PolicyConfig");
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set default audio device");
            return false;
        }
    }

    public string GetCurrentCodecName(string deviceName)
    {
        var codec = DetectCurrentCodec(deviceName);
        return codec switch
        {
            BluetoothCodec.SBC => "SBC (328 kbps)",
            BluetoothCodec.AAC => "AAC (256 kbps)",
            BluetoothCodec.AptX => "aptX (352 kbps)",
            BluetoothCodec.AptXHD => "aptX HD (576 kbps)",
            BluetoothCodec.AptXLL => "aptX Low Latency",
            BluetoothCodec.AptXAdaptive => "aptX Adaptive",
            BluetoothCodec.LDAC => "LDAC (до 990 kbps)",
            _ => "Неизвестно"
        };
    }

    private MMDevice? FindBluetoothEndpoint(string deviceName)
    {
        var endpoints = _audioEndpointService.GetBluetoothEndpoints();
        var btEndpoint = endpoints.FirstOrDefault(e =>
            e.FriendlyName.Contains(deviceName, StringComparison.OrdinalIgnoreCase) &&
            e.BluetoothProfile == BluetoothAudioProfile.A2dp);

        if (btEndpoint == null) return null;

        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

        return devices.FirstOrDefault(d =>
            d.FriendlyName.Contains(deviceName, StringComparison.OrdinalIgnoreCase));
    }

    private BluetoothCodec DetectCurrentCodec(string deviceName)
    {
        try
        {
            if (IsLDACEnabled())
                return BluetoothCodec.LDAC;

            if (IsAptXSupported())
                return BluetoothCodec.AptX;

            if (IsAACSupported())
                return BluetoothCodec.AAC;

            return BluetoothCodec.SBC;
        }
        catch
        {
            return BluetoothCodec.SBC;
        }
    }

    private static int EstimateBitrate(BluetoothCodec codec, int sampleRate)
    {
        return codec switch
        {
            BluetoothCodec.SBC => 328,
            BluetoothCodec.AAC => 256,
            BluetoothCodec.AptX => 352,
            BluetoothCodec.AptXHD => 576,
            BluetoothCodec.AptXLL => 352,
            BluetoothCodec.AptXAdaptive => 420,
            BluetoothCodec.LDAC => sampleRate >= 96000 ? 990 : 660,
            _ => 328
        };
    }

    private bool AreEnhancementsEnabled(string deviceId)
    {
        try
        {
            var deviceKey = deviceId.Replace("{", "").Replace("}", "").Replace(".", "");
            var regPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{deviceKey}\FxProperties";

            using var key = Registry.CurrentUser.OpenSubKey(regPath);
            if (key == null) return true;

            var value = key.GetValue("{d04e05a6-594b-4fb6-a80d-01af5eed7d1d},1");
            return value == null || !value.Equals(1);
        }
        catch
        {
            return true;
        }
    }

    private void SetEnhancementsEnabled(string deviceId, bool enabled)
    {
        try
        {
            var deviceKey = deviceId.Replace("{", "").Replace("}", "").Replace(".", "");
            var regPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{deviceKey}\FxProperties";

            using var key = Registry.CurrentUser.CreateSubKey(regPath);
            if (key != null)
            {
                key.SetValue("{d04e05a6-594b-4fb6-a80d-01af5eed7d1d},1", enabled ? 0 : 1, RegistryValueKind.DWord);
                Logger.Information("Audio enhancements {State}", enabled ? "enabled" : "disabled");
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to set enhancements state in registry");
        }
    }

    private bool IsAptXSupported()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Qualcomm\aptX");
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    private bool IsAptXHDSupported()
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

    private bool IsAACSupported()
    {
        return Environment.OSVersion.Version.Major >= 10;
    }

    public bool IsAACEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(BthAudioRegPath);
            if (key == null) return true;

            var value = key.GetValue("BluetoothAacEnable");
            if (value == null) return true;

            return Convert.ToInt32(value) != 0;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to check AAC status in registry");
            return true;
        }
    }

    public bool SetAACEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(BthAudioRegPath);
            if (key == null)
            {
                Logger.Error("Failed to create registry key for AAC settings");
                return false;
            }

            key.SetValue("BluetoothAacEnable", enabled ? 1 : 0, RegistryValueKind.DWord);
            Logger.Information("AAC codec {State} in registry. Reconnect BT device for changes to take effect.",
                enabled ? "enabled" : "disabled");
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            Logger.Error("Administrator rights required to change AAC setting");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to set AAC status in registry");
            return false;
        }
    }

    public async Task<bool> RestartBluetoothA2dpServiceAsync()
    {
        try
        {
            Logger.Information("Restarting Bluetooth stack to apply registry changes...");

            var adapterInfo = GetBluetoothAdapterInfo();
            if (!string.IsNullOrEmpty(adapterInfo.DeviceInstanceId))
            {
                Logger.Information("Trying PowerShell method for adapter: {InstanceId}", adapterInfo.DeviceInstanceId);

                if (await TryRestartViaPowerShellAsync(adapterInfo.DeviceInstanceId))
                {
                    Logger.Information("Bluetooth adapter restarted successfully via PowerShell");
                    return true;
                }

                Logger.Warning("PowerShell method failed, trying pnputil...");

                if (await TryRestartViaPnpUtilAsync(adapterInfo.DeviceInstanceId))
                {
                    Logger.Information("Bluetooth adapter restarted successfully via pnputil");
                    return true;
                }

                Logger.Warning("pnputil method failed (likely critical system device protection)");
            }

            Logger.Information("Trying Bluetooth service restart as fallback...");
            if (await TryRestartBluetoothServiceAsync())
            {
                Logger.Information("Bluetooth service restarted successfully");
                return true;
            }

            Logger.Error("All Bluetooth restart methods failed");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to restart Bluetooth stack");
            return false;
        }
    }

    private async Task<bool> TryRestartViaPowerShellAsync(string deviceInstanceId)
    {
        try
        {
            var disableScript = $"Disable-PnpDevice -InstanceId '{deviceInstanceId}' -Confirm:$false -ErrorAction Stop";
            var enableScript = $"Enable-PnpDevice -InstanceId '{deviceInstanceId}' -Confirm:$false -ErrorAction Stop";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{disableScript}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Logger.Information("PowerShell: Disabling Bluetooth adapter...");
            using var disableProcess = System.Diagnostics.Process.Start(psi);
            if (disableProcess == null) return false;

            var disableOutput = await disableProcess.StandardOutput.ReadToEndAsync();
            var disableError = await disableProcess.StandardError.ReadToEndAsync();
            await disableProcess.WaitForExitAsync();

            Logger.Information("PowerShell disable exit code: {Code}, output: {Output}, error: {Error}",
                disableProcess.ExitCode, disableOutput, disableError);

            if (disableProcess.ExitCode != 0 ||
                disableError.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
                disableError.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                disableError.Contains("denied", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Warning("PowerShell disable failed: {Error}", disableError);
                return false;
            }

            await Task.Delay(2000);

            psi.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{enableScript}\"";
            Logger.Information("PowerShell: Enabling Bluetooth adapter...");
            using var enableProcess = System.Diagnostics.Process.Start(psi);
            if (enableProcess == null) return false;

            var enableOutput = await enableProcess.StandardOutput.ReadToEndAsync();
            var enableError = await enableProcess.StandardError.ReadToEndAsync();
            await enableProcess.WaitForExitAsync();

            Logger.Information("PowerShell enable exit code: {Code}, output: {Output}, error: {Error}",
                enableProcess.ExitCode, enableOutput, enableError);

            return enableProcess.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "PowerShell restart method failed");
            return false;
        }
    }

    private async Task<bool> TryRestartViaPnpUtilAsync(string deviceInstanceId)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pnputil",
                Arguments = $"/disable-device \"{deviceInstanceId}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Logger.Information("pnputil: Disabling Bluetooth adapter...");
            using var disableProcess = System.Diagnostics.Process.Start(psi);
            if (disableProcess == null) return false;

            var disableOutput = await disableProcess.StandardOutput.ReadToEndAsync();
            var disableError = await disableProcess.StandardError.ReadToEndAsync();
            await disableProcess.WaitForExitAsync();

            var combinedOutput = disableOutput + disableError;
            Logger.Information("pnputil disable exit code: {Code}, output: {Output}",
                disableProcess.ExitCode, combinedOutput);

            if (disableProcess.ExitCode != 0 ||
                combinedOutput.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
                combinedOutput.Contains("critical", StringComparison.OrdinalIgnoreCase) ||
                combinedOutput.Contains("Cannot disable", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Warning("pnputil disable failed: {Output}", combinedOutput);
                return false;
            }

            await Task.Delay(2000);

            psi.Arguments = $"/enable-device \"{deviceInstanceId}\"";
            Logger.Information("pnputil: Enabling Bluetooth adapter...");
            using var enableProcess = System.Diagnostics.Process.Start(psi);
            if (enableProcess == null) return false;

            var enableOutput = await enableProcess.StandardOutput.ReadToEndAsync();
            var enableError = await enableProcess.StandardError.ReadToEndAsync();
            await enableProcess.WaitForExitAsync();

            Logger.Information("pnputil enable exit code: {Code}, output: {Output} {Error}",
                enableProcess.ExitCode, enableOutput, enableError);

            return enableProcess.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "pnputil restart method failed");
            return false;
        }
    }

    private async Task<bool> TryRestartBluetoothServiceAsync()
    {
        try
        {
            Logger.Information("Trying to restart Bluetooth A2DP driver via PowerShell...");

            var restartScript = @"
                $btDevices = Get-PnpDevice | Where-Object { $_.Class -eq 'Bluetooth' -or $_.FriendlyName -like '*Bluetooth*Audio*' -or $_.FriendlyName -like '*A2DP*' }
                foreach ($dev in $btDevices) {
                    Write-Output ""Restarting: $($dev.FriendlyName)""
                    try {
                        Disable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
                        Start-Sleep -Milliseconds 500
                        Enable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
                    } catch { }
                }
                # Перезапустим Bluetooth Audio Gateway
                $audioDevices = Get-PnpDevice | Where-Object { $_.FriendlyName -like '*headphone*' -or $_.FriendlyName -like '*headset*' }
                foreach ($dev in $audioDevices) {
                    Write-Output ""Restarting audio device: $($dev.FriendlyName)""
                    try {
                        Disable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
                        Start-Sleep -Milliseconds 500
                        Enable-PnpDevice -InstanceId $dev.InstanceId -Confirm:$false -ErrorAction SilentlyContinue
                    } catch { }
                }
                Write-Output 'Done'
            ";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{restartScript.Replace("\"", "\\\"").Replace("\r\n", " ").Replace("\n", " ")}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return false;

            var timeoutTask = Task.Delay(30000);
            var processTask = process.WaitForExitAsync();

            var completedTask = await Task.WhenAny(processTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                Logger.Warning("PowerShell restart timed out after 30 seconds");
                try { process.Kill(); } catch { }
                return false;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            Logger.Information("PowerShell restart exit code: {Code}, output: {Output}, error: {Error}",
                process.ExitCode, output, error);

            if (string.IsNullOrWhiteSpace(output) || output.Contains("Error") || output.Contains("denied"))
            {
                Logger.Information("PowerShell method didn't work, trying sc restart bthserv...");
                return await TryScRestartAsync();
            }

            return output.Contains("Done");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Bluetooth driver restart failed");
            return false;
        }
    }

    private async Task<bool> TryScRestartAsync()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "sc",
                Arguments = "stop bthserv",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Logger.Information("sc stop bthserv...");
            using var stopProcess = System.Diagnostics.Process.Start(psi);
            if (stopProcess == null) return false;

            if (!stopProcess.WaitForExit(10000))
            {
                Logger.Warning("sc stop bthserv timed out");
                try { stopProcess.Kill(); } catch { }
            }
            else
            {
                var stopOutput = await stopProcess.StandardOutput.ReadToEndAsync();
                Logger.Information("sc stop result: {Output}", stopOutput);
            }

            await Task.Delay(1000);

            psi.Arguments = "start bthserv";
            Logger.Information("sc start bthserv...");
            using var startProcess = System.Diagnostics.Process.Start(psi);
            if (startProcess == null) return false;

            if (!startProcess.WaitForExit(10000))
            {
                Logger.Warning("sc start bthserv timed out");
                try { startProcess.Kill(); } catch { }
                return false;
            }

            var startOutput = await startProcess.StandardOutput.ReadToEndAsync();
            Logger.Information("sc start result: {Output}", startOutput);

            return startProcess.ExitCode == 0 || startOutput.Contains("START_PENDING") || startOutput.Contains("RUNNING");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "sc restart failed");
            return false;
        }
    }

    public CodecRegistryInfo GetCodecRegistryInfo()
    {
        var info = new CodecRegistryInfo();

        try
        {
            info.AACEnabled = IsAACEnabled();

            info.AptXAvailable = IsAptXSupported();
            info.AptXHDAvailable = IsAptXHDSupported();

            info.LDACAvailable = IsLDACSupported();

            using var key = Registry.LocalMachine.OpenSubKey(BthAudioRegPath);
            if (key != null)
            {
                var preferredValue = key.GetValue("PreferredCodec");
                if (preferredValue != null)
                {
                    info.PreferredCodecValue = Convert.ToInt32(preferredValue);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to get codec registry info");
        }

        return info;
    }

    private bool IsLDACEnabled()
    {
        return IsLDACSupported();
    }

    private bool IsLDACSupported()
    {
        try
        {
            using var key1 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\LDAC");
            if (key1 != null) return true;

            using var key2 = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BthA2dp\Parameters\LDAC");
            return key2 != null;
        }
        catch
        {
            return false;
        }
    }

    private void SetPreferredCodecInRegistry(BluetoothCodec codec)
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(BthAudioRegPath);
            if (key != null)
            {
                var codecValue = codec switch
                {
                    BluetoothCodec.SBC => 0,
                    BluetoothCodec.AAC => 1,
                    BluetoothCodec.AptX => 2,
                    BluetoothCodec.AptXHD => 3,
                    BluetoothCodec.LDAC => 4,
                    _ => 0
                };

                key.SetValue("PreferredCodec", codecValue, RegistryValueKind.DWord);
                Logger.Information("Set preferred codec to {Codec} in registry", codec);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to set preferred codec in registry (may require admin rights)");
        }
    }

    public BluetoothAdapterInfo GetBluetoothAdapterInfo()
    {
        var info = new BluetoothAdapterInfo();

        try
        {
            using var btRadioKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTHUSB\Enum");
            if (btRadioKey != null)
            {
                var count = btRadioKey.GetValue("Count");
                if (count != null && Convert.ToInt32(count) > 0)
                {
                    var devicePath = btRadioKey.GetValue("0")?.ToString();
                    if (!string.IsNullOrEmpty(devicePath))
                    {
                        info.DevicePath = devicePath;
                    }
                }
            }

            using var enumKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum");
            if (enumKey != null && !string.IsNullOrEmpty(info.DevicePath))
            {
                info.DeviceInstanceId = info.DevicePath;

                using var deviceKey = enumKey.OpenSubKey(info.DevicePath);
                if (deviceKey != null)
                {
                    info.Name = deviceKey.GetValue("FriendlyName")?.ToString()
                                ?? deviceKey.GetValue("DeviceDesc")?.ToString()
                                ?? "Unknown";
                    info.Manufacturer = deviceKey.GetValue("Mfg")?.ToString() ?? "";
                }
            }

            if (string.IsNullOrEmpty(info.Name) || info.Name == "Unknown")
            {
                using var btEnumKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
                if (btEnumKey != null)
                {
                    foreach (var subKeyName in btEnumKey.GetSubKeyNames())
                    {
                        using var subKey = btEnumKey.OpenSubKey(subKeyName);
                        if (subKey == null) continue;

                        foreach (var instanceName in subKey.GetSubKeyNames())
                        {
                            using var instanceKey = subKey.OpenSubKey(instanceName);
                            if (instanceKey == null) continue;

                            var service = instanceKey.GetValue("Service")?.ToString();
                            if (service == "BTHUSB" || service == "BthEnum")
                            {
                                info.Name = instanceKey.GetValue("FriendlyName")?.ToString()
                                            ?? instanceKey.GetValue("DeviceDesc")?.ToString()
                                            ?? "Bluetooth Adapter";
                                info.Manufacturer = instanceKey.GetValue("Mfg")?.ToString() ?? "";
                                info.DeviceInstanceId = $"USB\\{subKeyName}\\{instanceName}";
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(info.DeviceInstanceId))
                            break;
                    }
                }
            }

            info.IsIntel = !string.IsNullOrEmpty(info.Name) &&
                           info.Name.Contains("Intel", StringComparison.OrdinalIgnoreCase);

            if (!info.IsIntel && !string.IsNullOrEmpty(info.Manufacturer))
            {
                info.IsIntel = info.Manufacturer.Contains("Intel", StringComparison.OrdinalIgnoreCase);
            }

            Logger.Information("Bluetooth adapter: {Name}, IsIntel: {IsIntel}", info.Name, info.IsIntel);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to get Bluetooth adapter info");
        }

        return info;
    }
}
