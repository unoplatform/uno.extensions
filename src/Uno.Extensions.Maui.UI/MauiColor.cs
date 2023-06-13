using Microsoft.Maui.Controls;

namespace Uno.Extensions.Maui;

[MarkupExtensionReturnType(ReturnType = typeof(NativeMauiColor))]
public class MauiColor : MauiExtensionBase
{
	public string Value { get; set; } = string.Empty;

	protected override void SetValue(View view, Type viewType, Type propertyType, BindableProperty property, string propertyName)
	{
		if (!string.IsNullOrEmpty(Value) || !NativeMauiColor.TryParse(Value, out var color))
		{
			var canLog = Logger.IsEnabled(LogLevel.Warning);
			if (string.IsNullOrEmpty(Value) && canLog)
			{
				Logger.LogWarning(Properties.Resources.NoColorValueProvided);
			}
			else if (canLog)
			{
				Logger.LogWarning(Properties.Resources.UnableToConvertValueToColor, Value);
			}
			return;
		}

		view.SetValue(property, color);
	}
}
