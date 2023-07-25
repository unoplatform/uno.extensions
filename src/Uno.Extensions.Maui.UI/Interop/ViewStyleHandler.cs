namespace Uno.Extensions.Maui.Interop;

internal class ViewStyleHandler : WinUIToMauiStyleHandler
{
	public override Type TargetType => typeof(Microsoft.Maui.Controls.View);

	public override MauiToWinUIStyleMapping? Process(DependencyProperty property, object value)
	{
		return property == Control.MarginProperty
			? new(Microsoft.Maui.Controls.View.MarginProperty, ConvertToMauiValue(value))
			: null;
	}
}
