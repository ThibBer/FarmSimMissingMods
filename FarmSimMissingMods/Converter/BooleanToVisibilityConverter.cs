using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace FarmSimMissingMods.Converter
{
    public class BooleanToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isVisible = value != null && (bool) value;
            var useCollapsed = parameter != null && parameter.ToString().ToLower().Equals("collapsed");

            return isVisible ? Visibility.Visible : (useCollapsed ? Visibility.Collapsed : Visibility.Hidden);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}