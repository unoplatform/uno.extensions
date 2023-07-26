using System.Text.RegularExpressions;

namespace MauiEmbedding.Presentation;

public class TitleConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
	public object Convert(object? value, Type targetType, object parameter, string language) => $"Converted: {value}";
	public object ConvertBack(object? value, Type targetType, object parameter, string language) => Regex.Replace(value?.ToString() ?? string.Empty, "(Converted: )", string.Empty);
}
