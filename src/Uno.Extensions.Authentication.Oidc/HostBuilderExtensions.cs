

namespace Uno.Extensions.Authentication.Oidc;

public static class HostBuilderExtensions
{
	public static IAuthenticationBuilder AddOidc(
		this IAuthenticationBuilder builder,
		Action<IOidcAuthenticationBuilder>? configure = default,
		string name = OidcAuthenticationProvider.DefaultName)
	{
		var hostBuilder = (builder as IBuilder)?.HostBuilder;
		if (hostBuilder is null)
		{
			return builder;
		}

		hostBuilder
			.UseConfiguration(configure: configBuilder =>
					configBuilder
						.Section<OidcClientOptions>(name)
				);


		var authBuilder = builder.AsBuilder<OidcAuthenticationBuilder>();
		configure?.Invoke(authBuilder);


		return builder
			.AddAuthentication<OidcAuthenticationProvider, OidcAuthenticationSettings>(
				name,
				authBuilder.Settings,
				(provider, settings) => {
					provider = provider with { Name = name, Settings = settings };
					provider.Build();
					return provider;
				});
	}

}
