using Microsoft.Extensions.Configuration;

namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static IAuthenticationBuilder AddOidc(
		this IAuthenticationBuilder builder,
		Action<IOidcAuthenticationBuilder>? configure = default,
		string name = OidcAuthenticationProvider.DefaultName)
	{
#if WINDOWS
		WinUIEx.WebAuthenticator.Init();
#endif

		var hostBuilder = (builder as IBuilder)?.HostBuilder;
		if (hostBuilder is null)
		{
			return builder;
		}

		var authBuilder = builder.AsBuilder<OidcAuthenticationBuilder>();
		if (authBuilder.Settings.Options == null)
		{
			authBuilder.Settings = new OidcAuthenticationSettings() { Options = new OidcClientOptions() };
		}
		hostBuilder.ConfigureServices((ctx, _) =>
			ctx.Configuration.GetSection(name).Bind(authBuilder.Settings.Options)
		);
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
