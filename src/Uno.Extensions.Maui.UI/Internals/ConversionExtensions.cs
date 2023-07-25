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

		try
		{
			if (input.ThemeDictionaries.Any())
			{
				input = input.ThemeDictionaries.TryGetValue("Default", out var dict) ?
							(dict as ResourceDictionary)! :
							(input.ThemeDictionaries.First().Value as ResourceDictionary)!;
			}

			foreach (var kvp in input)
			{
				try
				{
					if (input.MergedDictionaries.Any(x => x.ContainsKey(kvp.Key)))
					{
						continue;
					}

					TryAddValue(ref output, kvp.Key, kvp.Value);
				}
				catch (Exception e)
				{
					Console.WriteLine($"Failed to convert resource {kvp.Key} with value {kvp.Value} to Maui: {e}");
				}
			}
		}
		catch { } // TODO: Work out how to handle exceptions being raised when accessing dictionary with themeresources

		return output;
	}

	private static void TryAddValue(ref Microsoft.Maui.Controls.ResourceDictionary resources, object sourceKey, object value)
	{
		// NOTE: Interop was part of the POC and is out of scope for the MVP
		// if (value is Style winUIStyle)
		// {
		// 	// This needs to be nested to prevent further processing if we cannot generate a Maui Style
		// 	if(Interop.MauiInterop.TryGetStyle(winUIStyle, out var style) && style != null)
		// 	{
		// 		var key = sourceKey is string str ? str : style.TargetType.FullName;
		// 		resources[key] = style;
		// 	}
		// }
		// else
		if (sourceKey is string key && !string.IsNullOrEmpty(key) && !resources.ContainsKey(key))
		{
			var mauiValue = ConversionHelpers.ToMauiValue(value);
			resources[key] = mauiValue ?? value;
		}
	}
}
