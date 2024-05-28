
namespace Uno.Extensions;

/// <summary>
/// Provides extension methods for MSAL authentication to use with <see cref="IAuthenticationBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Adds MSAL authentication to the specified <see cref="IAuthenticationBuilder"/>.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IAuthenticationBuilder"/> to add MSAL authentication to.
	/// </param>
	/// <param name="configure">
	/// A delegate which can be used to configure the MSAL authentication provider that will be built. Optional.
	/// </param>
	/// <param name="name">
	/// The name of the authentication provider. This optional parameter defaults to "Msal".
	/// </param>
	/// <returns>
	/// The <see cref="IAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IAuthenticationBuilder AddMsal(
		this IAuthenticationBuilder builder,
		Action<IMsalAuthenticationBuilder>? configure = default,
		string name = MsalAuthenticationProvider.DefaultName)
	{
#if !UNO_EXT_MSAL
		return builder;
#else
		var hostBuilder = (builder as IBuilder)?.HostBuilder;
		if (hostBuilder is null)
		{
			return builder;
		}

		hostBuilder
			.UseConfiguration(configure: configBuilder =>
					configBuilder
						.Section<MsalConfiguration>(name)
				);


		var authBuilder = builder.AsBuilder<MsalAuthenticationBuilder>();
		configure?.Invoke(authBuilder);


		return builder
			.AddAuthentication<MsalAuthenticationProvider, MsalAuthenticationSettings>(
				name,
				authBuilder.Settings,
				(provider, settings) =>
				{
					provider = provider with { Name = name, Settings = settings };
					provider.Build();
					return provider;
				});
#endif
	}
}
