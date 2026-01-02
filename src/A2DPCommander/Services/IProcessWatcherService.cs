using BTAudioDriver.Models;

namespace BTAudioDriver.Services;

public interface IProcessWatcherService : IDisposable
{
    event EventHandler<ProfileChangeEventArgs>? ProfileChangeRequired;

    void StartWatching();

    void StopWatching();

    ProfileMode? GetRequiredProfile();

    void UpdateRules(IEnumerable<AppProfileRule> rules);
}

public class ProfileChangeEventArgs : EventArgs
{
    public ProfileMode? RequiredProfile { get; }

    public AppProfileRule? TriggeringRule { get; }

    public string? ProcessName { get; }

    public ProfileChangeEventArgs(ProfileMode? requiredProfile, AppProfileRule? triggeringRule = null, string? processName = null)
    {
        RequiredProfile = requiredProfile;
        TriggeringRule = triggeringRule;
        ProcessName = processName;
    }
}

public class ProcessEventArgs : EventArgs
{
    public string ProcessName { get; }
    public int ProcessId { get; }

    public ProcessEventArgs(string processName, int processId)
    {
        ProcessName = processName;
        ProcessId = processId;
    }
}
