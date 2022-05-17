namespace Uno.Extensions.Hosting
{
	public class HostingConstants
	{
		public static string AppConfigPrefixKey = "hostConfiguration:appConfigPrefix";
		public static string LaunchUrlKey = "hostConfiguration:launchUrl";
	}

	public class HostConfiguration
	{
		public string? AppConfigPrefix { get; set; }
		public string? LaunchUrl { get; set; }
	}
}
