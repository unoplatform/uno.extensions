using Uno.Extensions.Maui.Internals;
using UnoMusicApp.Helpers;

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
	public BindingMode BindingMode { get; set; } = BindingMode.Default;

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

	/// <inheritdoc/>
	protected override void SetValue(View view, Type viewType, Type propertyType, Microsoft.Maui.Controls.BindableProperty property, string propertyName)
	{
		if (string.IsNullOrEmpty(Path))
		{
			Path = ".";
		}

		ThreadHelpers.WhatThreadAmI();

		IMauiConverter? converter = null;
		if (Converter is IMauiConverter converterAsMauiConverter)
		{
			converter = converterAsMauiConverter;
		}
		else if (Converter is IWinUIConverter winUIConverter)
		{
			var value = ConversionHelpers.ToMauiValue(winUIConverter);
			if (value is IMauiConverter mauiConverter)
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
