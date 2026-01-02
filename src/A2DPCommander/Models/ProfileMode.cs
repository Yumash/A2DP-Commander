namespace BTAudioDriver.Models;

public enum ProfileMode
{
    Music,

    Calls,

    Auto
}

public class DeviceProfileState
{
    public required string DeviceId { get; init; }

    public required string DeviceName { get; init; }

    public ProfileMode CurrentMode { get; set; }

    public bool IsA2dpEnabled { get; set; }

    public bool IsHfpEnabled { get; set; }

    public string? HfpDeviceInstanceId { get; set; }

    public string? A2dpDeviceInstanceId { get; set; }
}
