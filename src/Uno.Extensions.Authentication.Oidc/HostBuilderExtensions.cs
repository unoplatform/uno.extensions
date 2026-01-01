using System.Diagnostics.CodeAnalysis;
using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions;

/// <summary>
/// Provides extension methods for OIDC authentication to use with <see cref="IAuthenticationBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
	internal const string RequiresDynamicCodeMessage = "Binding strongly typed objects to configuration values may require generating dynamic code at runtime. [From Array.CreateInstance() and others.]";
	internal const string RequiresUnreferencedCodeMessage = "Cannot statically analyze the type of instance so its members may be trimmed. [From TypeDescriptor.GetConverter() and others.]";

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
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IAuthenticationBuilder AddOidc(
		this IAuthenticationBuilder builder,
		Action<IOidcAuthenticationBuilder>? configure = default,
		string name = OidcAuthenticationProvider.DefaultName)
	{
#if WINDOWS
		WinUIEx.WebAuthenticator.CheckOAuthRedirectionActivation();
#endif

		var hostBuilder = (builder as IBuilder)?.HostBuilder;
		if (hostBuilder is null)
		{
			return builder;
		}

		hostBuilder = hostBuilder
			.ConfigureServices((ctx, services) =>
			{
				if (ctx.IsRegistered(nameof(AddOidc)))
				{
					return;
				}

				services
					.AddTransient<IBrowser, WebAuthenticatorBrowser>();
			});


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
