using BTAudioDriver.Models;

namespace BTAudioDriver.Services;

public interface IAudioQualityService
{
    AudioQualityInfo? GetCurrentQualityInfo(string deviceName);

    List<BluetoothCodec> GetSupportedCodecs(string deviceName);

    Task<bool> ApplyQualitySettingsAsync(string deviceName, AudioQualitySettings settings);

    Task<bool> DisableEnhancementsAsync(string deviceId);

    Task<bool> EnableEnhancementsAsync(string deviceId);

    Task<bool> SetDefaultDeviceAsync(string deviceId);

    string GetCurrentCodecName(string deviceName);

    bool IsAACEnabled();

    bool SetAACEnabled(bool enabled);

    CodecRegistryInfo GetCodecRegistryInfo();

    BluetoothAdapterInfo GetBluetoothAdapterInfo();

    bool ApplyMMCSSOptimizations();

    bool RevertMMCSSOptimizations();

    bool AreMMCSSOptimizationsApplied();
}
