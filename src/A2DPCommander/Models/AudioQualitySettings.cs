namespace BTAudioDriver.Models;

public class AudioQualitySettings
{
    public BluetoothCodec PreferredCodec { get; set; } = BluetoothCodec.Auto;

    public SampleRate PreferredSampleRate { get; set; } = SampleRate.Auto;

    public bool UseExclusiveMode { get; set; } = false;

    public bool DisableEnhancements { get; set; } = true;

    public int BufferSizeMs { get; set; } = 100;

    public bool SetAsDefaultDevice { get; set; } = true;

    public AudioThreadPriority ThreadPriority { get; set; } = AudioThreadPriority.High;
}

public enum BluetoothCodec
{
    Auto,

    SBC,

    AAC,

    AptX,

    AptXHD,

    AptXLL,

    AptXAdaptive,

    LDAC
}

public enum SampleRate
{
    Auto,

    Rate44100 = 44100,

    Rate48000 = 48000,

    Rate96000 = 96000
}

public enum AudioThreadPriority
{
    Normal,

    High,

    Critical
}

public class AudioQualityInfo
{
    public BluetoothCodec CurrentCodec { get; set; }

    public string CodecName { get; set; } = "Unknown";

    public int Bitrate { get; set; }

    public int SampleRate { get; set; }

    public int BitDepth { get; set; }

    public int Channels { get; set; }

    public int LatencyMs { get; set; }

    public bool IsExclusiveMode { get; set; }

    public bool EnhancementsEnabled { get; set; }

    public List<BluetoothCodec> SupportedCodecs { get; set; } = new();

    public override string ToString()
    {
        return $"{CodecName} @ {SampleRate / 1000.0:F1}kHz/{BitDepth}bit, {Bitrate}kbps, {LatencyMs}ms latency";
    }
}

public class CodecRegistryInfo
{
    public bool AACEnabled { get; set; } = true;

    public bool AptXAvailable { get; set; }

    public bool AptXHDAvailable { get; set; }

    public bool LDACAvailable { get; set; }

    public int? PreferredCodecValue { get; set; }

    public List<string> GetAvailableCodecs()
    {
        var codecs = new List<string> { "SBC" };

        if (AACEnabled)
            codecs.Add("AAC");

        if (AptXAvailable)
            codecs.Add("aptX");

        if (AptXHDAvailable)
            codecs.Add("aptX HD");

        if (LDACAvailable)
            codecs.Add("LDAC");

        return codecs;
    }

    public override string ToString()
    {
        return $"AAC: {(AACEnabled ? "ON" : "OFF")}, aptX: {(AptXAvailable ? "Yes" : "No")}, LDAC: {(LDACAvailable ? "Yes" : "No")}";
    }
}

public class BluetoothAdapterInfo
{
    public string Name { get; set; } = "Unknown";

    public string Manufacturer { get; set; } = "";

    public string DevicePath { get; set; } = "";

    public string DeviceInstanceId { get; set; } = "";

    public bool IsIntel { get; set; }

    public bool IsRealtek { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsActive { get; set; }

    public bool SupportsAAC { get; set; } = true;

    public bool SupportsAptX { get; set; }

    public bool SupportsAptXHD { get; set; }

    public bool SupportsLDAC { get; set; }

    public bool ShouldDisableAAC => IsIntel;

    public string SupportedCodecsDisplay
    {
        get
        {
            var codecs = new List<string> { "SBC" };
            if (SupportsAAC) codecs.Add("AAC");
            if (SupportsAptX) codecs.Add("aptX");
            if (SupportsAptXHD) codecs.Add("aptX HD");
            if (SupportsLDAC) codecs.Add("LDAC");
            return string.Join(", ", codecs);
        }
    }

    public string DisplayName => IsEnabled
        ? (IsActive ? $"● {Name}" : Name)
        : $"○ {Name} (отключён)";

    public override string ToString()
    {
        return $"{Name} [{SupportedCodecsDisplay}]";
    }
}
