

using Uno.Extensions.Authentication.Custom;

namespace Uno.Extensions.Authentication;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseAuthentication(
		this IHostBuilder builder,
		Action<ICustomAuthenticationBuilder>? configure = default)
	{
		var authBuilder = builder.AsBuilder<CustomAuthenticationBuilder>();

		configure?.Invoke(authBuilder);	

		return builder
			.ConfigureServices(services =>
			{
				services.AddSingleton(authBuilder.Settings);
				services.AddSingleton<IAuthenticationService, CustomAuthenticationService>();
			});
	}

}
