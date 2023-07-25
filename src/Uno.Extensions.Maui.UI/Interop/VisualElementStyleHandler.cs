namespace Uno.Extensions.Maui.Interop;

internal class VisualElementStyleHandler : WinUIToMauiStyleHandler
{
	public override Type TargetType => typeof(Microsoft.Maui.Controls.VisualElement);

	public override MauiToWinUIStyleMapping? Process(DependencyProperty property, object value)
	{
		return property == Control.BackgroundProperty
			? new(Microsoft.Maui.Controls.VisualElement.BackgroundProperty, ConvertToMauiValue(value))
			: null;
	}
}
