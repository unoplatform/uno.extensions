namespace Uno.Extensions.Maui;

/// <summary>
/// Provides a markup extension for creating a <see cref="Microsoft.Maui.Thickness"/> object from a string.
/// </summary>
[MarkupExtensionReturnType(ReturnType = typeof(Microsoft.Maui.Thickness))]
public class MauiThickness : MarkupExtension
{
	/// <summary>
	/// Gets or sets the string value to convert into a <see cref="Microsoft.Maui.Thickness"/> object. 
	/// </summary>
	public string Value { get; set; } = string.Empty;


#if MAUI_EMBEDDING
	static readonly Microsoft.Maui.Converters.ThicknessTypeConverter mauiThicknessConverter = new();

	/// <inheritdoc />
	protected override object ProvideValue()
	{
		if (string.IsNullOrEmpty(Value))
		{
			return global::Microsoft.Maui.Thickness.Zero;
		}

		return mauiThicknessConverter.ConvertFrom(null, CultureInfo.InvariantCulture, Value);
	}
#endif
}
