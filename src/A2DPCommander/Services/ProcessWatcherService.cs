using System.Diagnostics;
using System.Management;
using BTAudioDriver.Models;
using Serilog;

namespace BTAudioDriver.Services;

public class ProcessWatcherService : IProcessWatcherService
{
    private static readonly ILogger Logger = Log.ForContext<ProcessWatcherService>();

    private readonly Dictionary<string, AppProfileRule> _rules = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, AppProfileRule> _runningProcesses = new();
    private readonly object _lock = new();

    private ManagementEventWatcher? _startWatcher;
    private ManagementEventWatcher? _stopWatcher;
    private bool _isWatching;
    private bool _disposed;

    private ProfileMode? _currentRequiredProfile;

    public event EventHandler<ProfileChangeEventArgs>? ProfileChangeRequired;

    public ProcessWatcherService()
    {
        foreach (var rule in AppProfileRule.GetDefaultRules())
        {
            AddRule(rule);
        }
    }

    private void AddRule(AppProfileRule rule)
    {
        if (!rule.IsEnabled) return;

        var processName = rule.ProcessName.ToLowerInvariant();
        _rules[processName] = rule;

        if (processName.EndsWith(".exe"))
        {
            _rules[processName[..^4]] = rule;
        }
    }

    public void StartWatching()
    {
        if (_isWatching) return;

        try
        {
            CheckRunningProcesses();

            var startQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
            _startWatcher = new ManagementEventWatcher(startQuery);
            _startWatcher.EventArrived += OnProcessStarted;
            _startWatcher.Start();

            var stopQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace");
            _stopWatcher = new ManagementEventWatcher(stopQuery);
            _stopWatcher.EventArrived += OnProcessStopped;
            _stopWatcher.Start();

            _isWatching = true;
            Logger.Information("Process watcher started. Monitoring {Count} rules", _rules.Count / 2);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start process watcher. WMI may require admin rights.");
        }
    }

    public void StopWatching()
    {
        if (!_isWatching) return;

        try
        {
            _startWatcher?.Stop();
            _startWatcher?.Dispose();
            _startWatcher = null;

            _stopWatcher?.Stop();
            _stopWatcher?.Dispose();
            _stopWatcher = null;

            _isWatching = false;
            Logger.Information("Process watcher stopped");
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error stopping process watcher");
        }
    }

    public ProfileMode? GetRequiredProfile()
    {
        lock (_lock)
        {
            return _currentRequiredProfile;
        }
    }

    public void UpdateRules(IEnumerable<AppProfileRule> rules)
    {
        lock (_lock)
        {
            _rules.Clear();
            foreach (var rule in rules)
            {
                AddRule(rule);
            }
        }

        Logger.Information("Updated profile rules: {Count} rules", _rules.Count / 2);

        CheckRunningProcesses();
    }

    private void CheckRunningProcesses()
    {
        lock (_lock)
        {
            _runningProcesses.Clear();

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var rule = FindRule(process.ProcessName);
                    if (rule != null)
                    {
                        _runningProcesses[process.Id] = rule;
                        Logger.Debug("Found running target process: {Name} (PID: {Pid}) -> {Profile}",
                            process.ProcessName, process.Id, rule.TargetProfile);
                    }
                }
                catch
                {
                }
            }

            if (_runningProcesses.Count > 0)
            {
                Logger.Information("Found {Count} running target processes", _runningProcesses.Count);
            }

            RecalculateRequiredProfile(null);
        }
    }

    private AppProfileRule? FindRule(string processName)
    {
        _rules.TryGetValue(processName.ToLowerInvariant(), out var rule);
        return rule;
    }

    private void RecalculateRequiredProfile(string? triggerProcessName)
    {
        AppProfileRule? winningRule = null;

        foreach (var rule in _runningProcesses.Values)
        {
            if (winningRule == null || rule.Priority > winningRule.Priority)
            {
                winningRule = rule;
            }
        }

        var newProfile = winningRule?.TargetProfile;

        if (newProfile != _currentRequiredProfile)
        {
            var oldProfile = _currentRequiredProfile;
            _currentRequiredProfile = newProfile;

            Logger.Information("Profile requirement changed: {Old} -> {New} (triggered by: {Process}, rule: {Rule})",
                oldProfile?.ToString() ?? "Default",
                newProfile?.ToString() ?? "Default",
                triggerProcessName ?? "scan",
                winningRule?.DisplayName ?? "none");

            ProfileChangeRequired?.Invoke(this, new ProfileChangeEventArgs(newProfile, winningRule, triggerProcessName));
        }
    }

    private void OnProcessStarted(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var processName = e.NewEvent.Properties["ProcessName"].Value?.ToString() ?? "";
            var processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);

            var rule = FindRule(processName);
            if (rule != null)
            {
                lock (_lock)
                {
                    _runningProcesses[processId] = rule;
                    Logger.Information("Target process started: {Name} (PID: {Pid}) -> {Profile} [Priority: {Priority}]",
                        processName, processId, rule.TargetProfile, rule.Priority);

                    RecalculateRequiredProfile(processName);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error processing process start event");
        }
    }

    private void OnProcessStopped(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var processName = e.NewEvent.Properties["ProcessName"].Value?.ToString() ?? "";
            var processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);

            lock (_lock)
            {
                if (_runningProcesses.Remove(processId, out var rule))
                {
                    Logger.Information("Target process stopped: {Name} (PID: {Pid}) was {Profile}",
                        processName, processId, rule.TargetProfile);

                    RecalculateRequiredProfile(processName);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error processing process stop event");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopWatching();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
