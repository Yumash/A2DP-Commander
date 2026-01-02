namespace BTAudioDriver.Models;

public class AppProfileRule
{
    public string ProcessName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ProfileMode TargetProfile { get; set; } = ProfileMode.Calls;

    public int Priority { get; set; } = 100;

    public bool IsEnabled { get; set; } = true;

    public AppProfileRule() { }

    public AppProfileRule(string processName, string displayName, ProfileMode targetProfile, int priority = 100)
    {
        ProcessName = processName;
        DisplayName = displayName;
        TargetProfile = targetProfile;
        Priority = priority;
    }

    public static List<AppProfileRule> GetDefaultRules() =>
    [
        new("zoom.exe", "Zoom", ProfileMode.Calls, 100),
        new("Teams.exe", "Microsoft Teams", ProfileMode.Calls, 100),
        new("ms-teams.exe", "Microsoft Teams (New)", ProfileMode.Calls, 100),
        new("Skype.exe", "Skype", ProfileMode.Calls, 100),
        new("skypeapp.exe", "Skype (Store)", ProfileMode.Calls, 100),
        new("webex.exe", "Cisco Webex", ProfileMode.Calls, 100),
        new("telemost.exe", "Яндекс Телемост", ProfileMode.Calls, 100),

        new("discord.exe", "Discord", ProfileMode.Calls, 90),
        new("slack.exe", "Slack", ProfileMode.Calls, 90),
        new("Signal.exe", "Signal", ProfileMode.Calls, 80),
        new("Telegram.exe", "Telegram", ProfileMode.Calls, 70),
        new("viber.exe", "Viber", ProfileMode.Calls, 80),
        new("WhatsApp.exe", "WhatsApp", ProfileMode.Calls, 80),

        new("Spotify.exe", "Spotify", ProfileMode.Music, 50),
        new("AIMP.exe", "AIMP", ProfileMode.Music, 50),
        new("foobar2000.exe", "foobar2000", ProfileMode.Music, 50),
        new("YandexMusic.exe", "Яндекс Музыка", ProfileMode.Music, 50),
    ];

    public override string ToString() => $"{DisplayName} ({ProcessName}) → {TargetProfile} [P:{Priority}]";
}
