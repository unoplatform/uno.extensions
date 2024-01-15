namespace Uno.Extensions.Maui.Internals;

internal static class ConversionHelpers
{
	internal static object? ToMauiValue(object input)
	{
		return input switch
		{
			int valueAsInt => valueAsInt,
			double valueAsDouble => valueAsDouble,
			float valueAsFloat => valueAsFloat,
			IValueConverter converter => new UnoHostConverter(converter),
			SolidColorBrush solidColorBrush => new MauiSolidColorBrush(new MauiGraphicsColor(solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B, solidColorBrush.Color.A)),
			Color color => new MauiGraphicsColor(color.R, color.G, color.B, color.A),
			Thickness thickness => new Microsoft.Maui.Thickness(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom),
			HorizontalAlignment horizontalAlignment => GetLayoutOptions(horizontalAlignment),
			VerticalAlignment verticalAlignment => GetLayoutOptions(verticalAlignment),
			_ => null
		};
	}

	private static LayoutOptions GetLayoutOptions(HorizontalAlignment alignment) =>
		alignment switch
		{
			HorizontalAlignment.Center => LayoutOptions.Center,
			HorizontalAlignment.Right => LayoutOptions.End,
			HorizontalAlignment.Left => LayoutOptions.Start,
			_ => LayoutOptions.Fill
		};

	private static LayoutOptions GetLayoutOptions(VerticalAlignment alignment) =>
		alignment switch
		{
			VerticalAlignment.Center => LayoutOptions.Center,
			VerticalAlignment.Bottom => LayoutOptions.End,
			VerticalAlignment.Top => LayoutOptions.Start,
			_ => LayoutOptions.Fill
		};

	private class UnoHostConverter : IMauiValueConverter
	{
		private IValueConverter _converter { get; }

		public UnoHostConverter(IValueConverter converter) => _converter = converter;

		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
			_converter.Convert(value, targetType, parameter, culture.Name);

		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
			_converter.ConvertBack(value, targetType, parameter, culture.Name);
	}
}
