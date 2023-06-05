namespace Uno.Extensions.Maui;

[MarkupExtensionReturnType(ReturnType = typeof(Microsoft.Maui.Thickness))]
public class MauiThickness : MarkupExtension
{
	public string Value { get; set; } = string.Empty;

	protected override object ProvideValue()
	{
		if (string.IsNullOrEmpty(Value))
		{
			return global::Microsoft.Maui.Thickness.Zero;
		}

		var temp = Value.Split(',')
			.Select(x => x.Trim())
			.Select(x => double.TryParse(x, out var thickness) ? (object)thickness : x);

		var values = temp.OfType<double>().ToArray();
		if (temp.Count() != values.Length)
		{
			throw new MauiEmbeddingException($"Unable to parse the Thickness string '{Value}'.");
		}

		return values.Length switch
		{
			1 => new Microsoft.Maui.Thickness(values[0]),
			2 => new Microsoft.Maui.Thickness(values[0], values[1]),
			4 => new Microsoft.Maui.Thickness(values[0], values[1], values[2], values[3]),
			_ => throw new MauiEmbeddingException($"The Thickness string '{Value}' has an invalid number of arguments")
		};
	}
}
