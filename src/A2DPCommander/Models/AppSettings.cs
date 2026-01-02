namespace BTAudioDriver.Models;

public class AppSettings
{
    public string? DefaultDeviceId { get; set; }

    public string DefaultDeviceName { get; set; } = "";

    public string Language { get; set; } = "ru";

    public bool AutoStart { get; set; }

    public ProfileMode DefaultMode { get; set; } = ProfileMode.Music;

    public bool ShowNotifications { get; set; } = true;

    public bool AutoSwitchOnConnect { get; set; } = true;

    public HotkeyConfig? SwitchModeHotkey { get; set; }

    public bool AutoSwitchByApp { get; set; } = false;

    public List<AppProfileRule> ProfileRules { get; set; } = AppProfileRule.GetDefaultRules();

    [Obsolete("Use ProfileRules instead")]
    public List<string>? HfpApplications { get; set; }

    public AudioQualitySettings AudioQuality { get; set; } = new();
}

public class HotkeyConfig
{
    public ModifierKeys Modifiers { get; set; }

    public int VirtualKeyCode { get; set; }

    public bool IsEnabled { get; set; }

    public override string ToString()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Win)) parts.Add("Win");

        var keyName = ((System.Windows.Forms.Keys)VirtualKeyCode).ToString();
        parts.Add(keyName);

        return string.Join(" + ", parts);
    }
}

[Flags]
public enum ModifierKeys
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}
