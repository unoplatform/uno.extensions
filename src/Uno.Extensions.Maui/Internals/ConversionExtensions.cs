namespace Uno.Extensions.Maui.Internals;

internal static class ConversionExtensions
{
	public static Microsoft.Maui.Controls.ResourceDictionary ToMauiResources(this ResourceDictionary input)
	{
		var output = new Microsoft.Maui.Controls.ResourceDictionary();
		foreach (var merged in input.MergedDictionaries)
		{
			output.MergedDictionaries.Add(merged.ToMauiResources());
		}

		foreach (var key in input.Keys)
		{
			if (input.MergedDictionaries.Any(x => x.ContainsKey(key)))
			{
				continue;
			}

			TryAddValue(ref output, key, input[key]);
		}

		return output;
	}

	private static void TryAddValue(ref Microsoft.Maui.Controls.ResourceDictionary resources, object sourceKey, object value)
	{
		if (value is Style winUIStyle)
		{
			// This needs to be nested to prevent further processing if we cannot generate a Maui Style
			if(Interop.MauiInterop.TryGetStyle(winUIStyle, out var style) && style != null)
			{
				var key = sourceKey is string str ? str : style.TargetType.FullName;
				resources[key] = style;
			}
		}
		else if (sourceKey is string key && !string.IsNullOrEmpty(key) && !resources.ContainsKey(key))
		{
			var mauiValue = ConversionHelpers.ToMauiValue(value);
			resources[key] = mauiValue ?? value;
		}
	}
}
