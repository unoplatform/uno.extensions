namespace Uno.Extensions.Configuration;

internal static class AppConfiguration
{
	public const string Prefix = "appsettings";
	public const string FileName = $"{Prefix}.json";
	public const string FileNameTemplate = $"{Prefix}.{{0}}.json";
}
