namespace Uno.Extensions.Maui;

/// <summary>
/// A binding extension for Maui apps.
/// </summary>
[ContentProperty(Name = nameof(Path))]
public class MauiBinding : MauiExtensionBase
{
	/// <summary>
	/// The path to the bound property.
	/// </summary>
	public string? Path { get; set; }

	/// <summary>
	/// The direction of the binding mode.
	/// </summary>
	public MauiBindingMode BindingMode { get; set; } = MauiBindingMode.Default;

	/// <summary>
	/// The string format of the bound property.
	/// </summary>
	public string? StringFormat { get; set; }

	/// <summary>
	/// The converter for the bound property.
	/// </summary>
	public object? Converter { get; set; }

	/// <summary>
	/// The parameter for the converter.
	/// </summary>
	public object? ConverterParameter { get; set; }

	/// <summary>
	/// The source object for the bound property.
	/// </summary>
	public object? Source { get; set; }

#if MAUI_EMBEDDING
	/// <inheritdoc/>
	protected override void SetValue(View view, Type viewType, Type propertyType, BindableProperty property, string propertyName)
	{
		if (string.IsNullOrEmpty(Path))
		{
			Path = ".";
		}

		IMauiValueConverter? converter = null;
		if (Converter is IMauiValueConverter converterAsMauiConverter)
		{
			converter = converterAsMauiConverter;
		}
		else if (Converter is IValueConverter winUIConverter)
		{
			var value = ConversionHelpers.ToMauiValue(winUIConverter);
			if (value is IMauiValueConverter mauiConverter)
			{
				converter = mauiConverter;
			}
		}

		var binding = new MauiControlsBinding(Path,
			mode: BindingMode,
			converter: converter,
			converterParameter: ConverterParameter,
			stringFormat: StringFormat,
			source: Source);

		view.SetBinding(property, binding);
	}
#endif
}
