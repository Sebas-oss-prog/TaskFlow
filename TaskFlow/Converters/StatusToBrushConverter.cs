using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TaskFlow.Converters
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Новая" => new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    "В работе" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    "На проверке" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),
                    "Выполнено" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    "Просрочено" => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
