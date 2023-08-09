namespace Uno.Extensions;

/// <summary>
/// Provides extension methods for OIDC authentication to use with <see cref="IAuthenticationBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Adds OIDC authentication to the specified <see cref="IAuthenticationBuilder"/>.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IAuthenticationBuilder"/> to add OIDC authentication to.
	/// </param>
	/// <param name="configure">
	/// A delegate which can be used to configure the OIDC authentication provider that will be built. Optional.
	/// </param>
	/// <param name="name">
	/// The name of the authentication provider. This optional parameter defaults to "Oidc".
	/// </param>
	/// <returns>
	/// The <see cref="IAuthenticationBuilder"/> that was passed in.
	/// </returns>
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
				(provider, settings) =>
				{
					provider = provider with { Name = name, Settings = settings };
					provider.Build();
					return provider;
				});
	}

}
