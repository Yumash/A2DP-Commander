using System.Globalization;
using System.Windows.Data;
using BTAudioDriver.Localization;
using BTAudioDriver.Models;

namespace BTAudioDriver.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();

        return enumValue?.Equals(targetValue, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || !boolValue || parameter == null)
            return System.Windows.Data.Binding.DoNothing;

        var parameterString = parameter.ToString();
        if (string.IsNullOrEmpty(parameterString))
            return System.Windows.Data.Binding.DoNothing;

        return Enum.Parse(targetType, parameterString);
    }
}

public class ProfileModeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ProfileMode mode)
        {
            return mode switch
            {
                ProfileMode.Music => Strings.Mode_Music,
                ProfileMode.Calls => Strings.Mode_Calls,
                _ => value.ToString() ?? ""
            };
        }
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            if (str == Strings.Mode_Music || str.Equals("Music", StringComparison.OrdinalIgnoreCase))
                return ProfileMode.Music;
            if (str == Strings.Mode_Calls || str.Equals("Calls", StringComparison.OrdinalIgnoreCase))
                return ProfileMode.Calls;
        }
        return ProfileMode.Music;
    }
}
