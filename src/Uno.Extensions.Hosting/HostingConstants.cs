namespace Uno.Extensions.Hosting
{
	public class HostingConstants
	{
		public static string AppSettingsPrefixKey = "hostConfiguration:appSettingsPrefix";
		public static string LaunchUrlKey = "hostConfiguration:launchUrl";
	}

	public class HostConfiguration
	{
		public string AppSettingsPrefix { get; set; }
		public string LaunchUrl { get; set; }
	}
}
