using BTAudioDriver.Localization;

namespace BTAudioDriver.Models;

public class LanguageOption
{
    public required string Code { get; init; }
    public required string Name { get; init; }
}

public class ThreadPriorityOption
{
    private static List<ThreadPriorityOption>? _cachedList;

    public AudioThreadPriority Value { get; init; }

    public string DisplayName => Value switch
    {
        AudioThreadPriority.Normal => Strings.CurrentLanguage == Language.Russian ? "Обычный" : "Normal",
        AudioThreadPriority.High => Strings.CurrentLanguage == Language.Russian ? "Высокий" : "High",
        AudioThreadPriority.Critical => Strings.CurrentLanguage == Language.Russian ? "Критический" : "Critical",
        _ => Value.ToString()
    };

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj) => obj is ThreadPriorityOption other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();

    public static List<ThreadPriorityOption> GetAll()
    {
        _cachedList ??= new List<ThreadPriorityOption>
        {
            new ThreadPriorityOption { Value = AudioThreadPriority.Normal },
            new ThreadPriorityOption { Value = AudioThreadPriority.High },
            new ThreadPriorityOption { Value = AudioThreadPriority.Critical }
        };
        return _cachedList;
    }
}
