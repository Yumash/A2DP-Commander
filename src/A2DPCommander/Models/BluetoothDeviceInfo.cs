namespace BTAudioDriver.Models;

public class BluetoothDeviceInfo
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public ulong BluetoothAddress { get; init; }

    public string MacAddress => FormatMacAddress(BluetoothAddress);

    public bool IsConnected { get; set; }

    public bool IsPaired { get; init; }

    public bool SupportsA2dp { get; set; }

    public bool SupportsHfp { get; set; }

    public bool SupportsAvrcp { get; set; }

    public bool IsAudioDevice => SupportsA2dp || SupportsHfp;

    private static string FormatMacAddress(ulong address)
    {
        var bytes = BitConverter.GetBytes(address);
        return string.Join(":", bytes.Take(6).Reverse().Select(b => b.ToString("X2")));
    }

    public override string ToString() => $"{Name} ({MacAddress}) - {(IsConnected ? "Connected" : "Disconnected")}";
}
