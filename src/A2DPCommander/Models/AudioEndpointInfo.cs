namespace BTAudioDriver.Models;

public enum BluetoothAudioProfile
{
    Unknown,

    A2dp,

    Hfp
}

public class AudioEndpointInfo
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string FriendlyName { get; init; }

    public bool IsPlayback { get; init; }

    public bool IsRecording { get; init; }

    public bool IsActive { get; init; }

    public bool IsDefault { get; set; }

    public bool IsBluetooth { get; init; }

    public BluetoothAudioProfile BluetoothProfile { get; init; }

    public string? BluetoothDeviceId { get; init; }

    public bool IsA2dp => BluetoothProfile == BluetoothAudioProfile.A2dp ||
                          (IsBluetooth && IsPlayback && BluetoothProfile != BluetoothAudioProfile.Hfp);

    public bool IsHfp => BluetoothProfile == BluetoothAudioProfile.Hfp;

    public override string ToString() => $"{FriendlyName} ({BluetoothProfile})";
}
