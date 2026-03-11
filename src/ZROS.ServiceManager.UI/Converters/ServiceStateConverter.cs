using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ZROS.ServiceManager.Models;

namespace ZROS.ServiceManager.UI.Converters
{
    /// <summary>Converts a <see cref="ServiceState"/> value to a display-friendly string.</summary>
    [ValueConversion(typeof(ServiceState), typeof(string))]
    public class ServiceStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is ServiceState s ? s.ToString() : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Enum.TryParse<ServiceState>(value?.ToString(), out var result) ? result : ServiceState.Stopped;
    }

    /// <summary>Converts a <see cref="ServiceState"/> value to a status colour brush.</summary>
    [ValueConversion(typeof(ServiceState), typeof(Brush))]
    public class ServiceStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ServiceState state) return Brushes.Gray;
            return state switch
            {
                ServiceState.Running  => new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60)),
                ServiceState.Faulted  => new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)),
                ServiceState.Starting => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)),
                ServiceState.Stopping => new SolidColorBrush(Color.FromRgb(0xF3, 0x9C, 0x12)),
                ServiceState.Failed   => new SolidColorBrush(Color.FromRgb(0x8E, 0x44, 0xAD)),
                _                     => new SolidColorBrush(Color.FromRgb(0x95, 0xA5, 0xA6))
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>Converts a bool to <see cref="Visibility"/>. True → Visible, False → Collapsed.</summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility.Visible;
    }

    /// <summary>Converts a bool to <see cref="Visibility"/>. True → Collapsed, False → Visible.</summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility.Collapsed;
    }
}
