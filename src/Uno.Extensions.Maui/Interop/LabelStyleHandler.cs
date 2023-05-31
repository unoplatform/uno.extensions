namespace Uno.Extensions.Maui.Interop;

internal class LabelStyleHandler : WinUIToMauiStyleHandler
{
	public override Type TargetType => typeof(Microsoft.Maui.Controls.Label);

	public override (Microsoft.Maui.Controls.BindableProperty Property, object? Value)? Process(DependencyProperty property, object value)
	{
		if (property == Control.ForegroundProperty && TryConvertValue(value, out var converted) && converted is Microsoft.Maui.Controls.SolidColorBrush brush)
		{
			return (Microsoft.Maui.Controls.Label.TextColorProperty, brush.Color);
		}
		else if (property == TextBlock.FontSizeProperty)
		{
			return (Microsoft.Maui.Controls.Label.FontSizeProperty, ConvertToMauiValue(value));
		}

		return null;
	}
}
