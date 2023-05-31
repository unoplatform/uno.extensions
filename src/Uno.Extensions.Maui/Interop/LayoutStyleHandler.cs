namespace Uno.Extensions.Maui.Interop;

internal class LayoutStyleHandler : WinUIToMauiStyleHandler
{
	public override Type TargetType => typeof(Microsoft.Maui.Controls.Layout);

	public override (Microsoft.Maui.Controls.BindableProperty Property, object? Value)? Process(DependencyProperty property, object value)
	{
		return property == Control.PaddingProperty
			? (Microsoft.Maui.Controls.Layout.PaddingProperty, ConvertToMauiValue(value))
			: null;
	}
}
