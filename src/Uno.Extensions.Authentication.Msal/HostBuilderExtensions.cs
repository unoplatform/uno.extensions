using Microsoft.Extensions.Hosting;
using Uno.Extensions.Configuration;

namespace Uno.Extensions.Authentication.MSAL;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseMsalAuthentication(
		this IHostBuilder builder)
	{
		return builder.UseAuthentication((IMsalAuthenticationBuilder builder) => { });
	}
	public static IHostBuilder UseAuthentication(
	this IHostBuilder builder,
	Action<IMsalAuthenticationBuilder>? configure = default)
	{
		var authBuilder = builder.AsBuilder<MsalAuthenticationBuilder>();

		configure?.Invoke(authBuilder);

		return builder
			.UseConfiguration(configure: configBuilder =>
					configBuilder
						.Section<MsalConfiguration>()
				)

			.UseAuthentication<MsalAuthenticationService, MsalAuthenticationSettings>(authBuilder.Settings);
	}
}
