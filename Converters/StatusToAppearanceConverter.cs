using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Controls;

namespace NodeLabFarm.Converters
{
    public class StatusToAppearanceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Ready" => ControlAppearance.Info,
                    "Running" => ControlAppearance.Primary,
                    "Completed" => ControlAppearance.Success,
                    "Error" => ControlAppearance.Danger,
                    _ => ControlAppearance.Secondary
                };
            }
            return ControlAppearance.Secondary;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
