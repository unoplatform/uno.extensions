namespace TestHarness.Converters;

public class StringFormatter : IValueConverter
{
/// <summary>
/// Format to be used.
/// </summary>
public string? Format { get; set; }

public object? Convert(object value, Type targetType, object parameter, string language)
{
	if (value is null) return null;
	if ((Format ?? parameter as string) is not { } format) return value.ToString();

	if (value is int count)
	{
		// Handle singular form
		format = count == 1 && format.EndsWith("s") ? format.Remove(format.Length - 1) : format;
	}

	return string.Format(CultureInfo.CurrentUICulture, format, value);
}

public object ConvertBack(object value, Type targetType, object parameter, string language)
	 => throw new NotSupportedException("Only one-way conversion is supported.");
}
