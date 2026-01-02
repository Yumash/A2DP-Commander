using BTAudioDriver.Models;

namespace BTAudioDriver.Services;

public interface IAudioEndpointService : IDisposable
{
    event EventHandler<AudioEndpointInfo>? EndpointAdded;

    event EventHandler<AudioEndpointInfo>? EndpointRemoved;

    event EventHandler<AudioEndpointInfo>? DefaultDeviceChanged;

    IReadOnlyList<AudioEndpointInfo> GetPlaybackEndpoints();

    IReadOnlyList<AudioEndpointInfo> GetRecordingEndpoints();

    IReadOnlyList<AudioEndpointInfo> GetBluetoothEndpoints();

    IReadOnlyList<AudioEndpointInfo> GetEndpointsForBluetoothDevice(string deviceName);

    AudioEndpointInfo? GetDefaultPlaybackDevice();

    AudioEndpointInfo? GetDefaultRecordingDevice();

    bool SetDefaultPlaybackDevice(string deviceId);

    bool SetDefaultRecordingDevice(string deviceId);

    void Refresh();
}
