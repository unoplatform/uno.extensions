using Uno.Extensions.Hosting;

namespace Uno.Extensions.Navigation;

public static class HostConfigurationExtensions
{
	public static Route? LaunchRoute(this HostConfiguration configuration)
	{
		var launchUrl = configuration.LaunchUrl;

		if (!string.IsNullOrWhiteSpace(launchUrl) && launchUrl.StartsWith("http"))
		{
			var url = new UriBuilder(launchUrl);
			var query = url.Query;
			var path = (url.Path + (!string.IsNullOrWhiteSpace(query) ? "?" : "") + query + "").TrimStart('/');
			if (!string.IsNullOrWhiteSpace(path))
			{
				return path.AsRoute();
			}
		}
		else
		{
			return launchUrl.AsRoute();
		}

		return null;
	}
}
