using Uno.Extensions.Maui.Internals;

namespace Uno.Extensions.Maui;

[ContentProperty(Name = nameof(Path))]
public class MauiBinding : MauiExtensionBase
{
	public string? Path { get; set; }

	public BindingMode BindingMode { get; set; } = BindingMode.Default;

	public string? StringFormat { get; set; }

	public object? Converter { get; set; }

	public object? ConverterParameter { get; set; }

	public object? Source { get; set; }

	protected override void SetValue(View view, Type viewType, Type propertyType, Microsoft.Maui.Controls.BindableProperty property, string propertyName)
	{
		if (string.IsNullOrEmpty(Path))
		{
			Path = ".";
		}


		MauiConverter? converter = null;
		if (Converter is MauiConverter converterAsMauiConverter)
		{
			converter = converterAsMauiConverter;
		}
		else if (Converter is WinUIConverter winUIConverter)
		{
			var value = ConversionHelpers.ToMauiValue(winUIConverter);
			if (value is MauiConverter mauiConverter)
			{
				converter = mauiConverter;
			}
		}

		var binding = new NativeMauiBinding(Path,
			mode: BindingMode,
			converter: converter,
			converterParameter: ConverterParameter,
			stringFormat: StringFormat,
			source: Source);

		view.SetBinding(property, binding);
	}
}
