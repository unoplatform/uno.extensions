using System;
using Nventive.View.Converters;

namespace ApplicationTemplate.Views
{
    /// <summary>
    /// Use this converter to debug data bindings in your xaml.
    /// </summary>
    public class DebugConverter : ConverterBase
    {
        protected override object Convert(object value, Type targetType, object parameter)
        {
            // Put a breakpoint here to inspect values from the ViewModel to the View.
            return value;
        }

        protected override object ConvertBack(object value, Type targetType, object parameter)
        {
            // Put a breakpoint here to inspect values from the View to the ViewModel.
            return value;
        }
    }
}
