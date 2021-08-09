using System;
using System.Globalization;
//-:cnd:noEmit
#if !WINUI
//+:cnd:noEmit
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
//-:cnd:noEmit
#else
//+:cnd:noEmit
using ApplicationTemplate.Presentation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit


namespace ApplicationTemplate.Views
{

    public abstract class ConverterBase : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string culture)
        {
            return Convert(value, targetType, parameter);
        }

        protected abstract object Convert(object value, Type targetType, object parameter);

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string culture)
        {
            return ConvertBack(value, targetType, parameter);
        }

        protected virtual object ConvertBack(object value, Type targetType, object parameter)
        {
            throw new NotSupportedException();
        }
    }

    public class FromNullableBoolToVisibilityConverter : ConverterBase
    {
        public VisibilityIfTrue VisibilityIfTrue { get; set; }

        public FromNullableBoolToVisibilityConverter()
        {
            VisibilityIfTrue = VisibilityIfTrue.Visible;
        }

        protected override object Convert(object value, Type targetType, object parameter)
        {
            if (parameter != null)
            {
                throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
            }
            bool num = VisibilityIfTrue == VisibilityIfTrue.Collapsed;
            Visibility visibility = (num ? Visibility.Collapsed : Visibility.Visible);
            Visibility visibility2 = ((!num) ? Visibility.Collapsed : Visibility.Visible);
            if (value != null && !(value is bool))
            {
                throw new ArgumentException($"Value must either be null or of type bool. Got {value} ({value.GetType().FullName})");
            }
            return (value != null && System.Convert.ToBoolean(value, CultureInfo.InvariantCulture)) ? visibility : visibility2;
        }

        protected override object ConvertBack(object value, Type targetType, object parameter)
        {
            if (value == null)
            {
                return null;
            }
            if (parameter != null)
            {
                throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
            }
            Visibility visibility = ((VisibilityIfTrue == VisibilityIfTrue.Collapsed) ? Visibility.Collapsed : Visibility.Visible);
            Visibility visibility2 = (Visibility)value;
            return visibility.Equals(visibility2);
        }
    }


    public class FromNullableBoolToCustomValueConverter : ConverterBase
    {
        public object NullOrFalseValue { get; set; }

        public object TrueValue { get; set; }

        protected override object Convert(object value, Type targetType, object parameter)
        {
            if (parameter != null)
            {
                throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
            }
            if (value != null && !(value is bool))
            {
                throw new ArgumentException($"Value must either be null or of type bool. Got {value} ({value.GetType().FullName})");
            }
            if (value == null || !System.Convert.ToBoolean(value, CultureInfo.InvariantCulture))
            {
                return NullOrFalseValue;
            }
            return TrueValue;
        }

        protected override object ConvertBack(object value, Type targetType, object parameter)
        {
            if (parameter != null)
            {
                throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
            }
            if (object.Equals(TrueValue, NullOrFalseValue))
            {
                throw new InvalidOperationException("Cannot convert back if both custom values are the same");
            }
            return (TrueValue != null) ? value.Equals(TrueValue) : (!value.Equals(NullOrFalseValue));
        }
    }

    public class FromEmptyStringToCustomValueConverter : ConverterBase
    {
        public object ValueIfEmpty { get; set; }

        public object ValueIfNotEmpty { get; set; }

        protected override object Convert(object value, Type targetType, object parameter)
        {
            if (string.IsNullOrEmpty(value as string))
            {
                return ValueIfEmpty;
            }
            return ValueIfNotEmpty;
        }
    }

    public enum VisibilityIfTrue
    {
        Visible,
        Collapsed
    }


}
