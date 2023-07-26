namespace Uno.Extensions.Maui.Interop;

internal class LayoutStyleHandler : WinUIToMauiStyleHandler
{
	public override Type TargetType => typeof(Microsoft.Maui.Controls.Layout);

	public override MauiToWinUIStyleMapping? Process(DependencyProperty property, object value)
	{
		return property == Control.PaddingProperty
			? new(Microsoft.Maui.Controls.Layout.PaddingProperty, ConvertToMauiValue(value))
			: null;
	}
}
