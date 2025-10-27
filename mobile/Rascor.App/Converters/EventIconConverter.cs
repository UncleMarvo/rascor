using System.Globalization;

namespace Rascor.App.Converters;

public class EventIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value?.ToString() == "Enter")
            return "🟢";
        else if (value?.ToString() == "Exit")
            return "🔴";
        else
            return "⚪";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}