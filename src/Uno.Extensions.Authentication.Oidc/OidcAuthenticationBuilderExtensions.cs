using System.ComponentModel;

namespace Uno.Extensions.Authentication;

/// <summary>
/// Provides OIDC-related extension methods for <see cref="IOidcAuthenticationBuilder"/>.
/// </summary>
public static class OidcAuthenticationBuilderExtensions
{
	/// <summary>
	/// Configures the OIDC authentication feature to be built with the specified authority.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IOidcAuthenticationBuilder"/> to configure.
	/// </param>
	/// <param name="authority">
	/// The authority to use for OIDC authentication.
	/// </param>
	/// <returns>
	/// The <see cref="IOidcAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IOidcAuthenticationBuilder Authority(
		this IOidcAuthenticationBuilder builder,
		string authority)
		=> builder.ConfigureOidcClientOptions(options => options.Authority = authority);

	/// <summary>
	/// Configures the OIDC authentication feature to be built with the specified client ID.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IOidcAuthenticationBuilder"/> to configure.
	/// </param>
	/// <param name="clientId">
	/// The client ID to use for OIDC authentication.
	/// </param>
	/// <returns>
	/// The <see cref="IOidcAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IOidcAuthenticationBuilder ClientId(
		this IOidcAuthenticationBuilder builder,
		string clientId)
		=> builder.ConfigureOidcClientOptions(options => options.ClientId = clientId);

	/// <summary>
	/// Configures the OIDC authentication feature to be built with the specified client secret.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IOidcAuthenticationBuilder"/> to configure.
	/// </param>
	/// <param name="clientSecret">
	/// The client secret to use for OIDC authentication.
	/// </param>
	/// <returns>
	/// The <see cref="IOidcAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IOidcAuthenticationBuilder ClientSecret(
		this IOidcAuthenticationBuilder builder,
		string clientSecret)
		=> builder.ConfigureOidcClientOptions(options => options.ClientSecret = clientSecret);

	/// <summary>
	/// Configures the OIDC authentication feature to be built with the specified scope.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IOidcAuthenticationBuilder"/> to configure.
	/// </param>
	/// <param name="scope">
	/// The scope to use for OIDC authentication.
	/// </param>
	/// <returns>
	/// The <see cref="IOidcAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IOidcAuthenticationBuilder Scope(
		this IOidcAuthenticationBuilder builder,
		string scope)
		=> builder.ConfigureOidcClientOptions(options => options.Scope = scope);

	/// <summary>
	/// Configures the OIDC authentication feature to be built with the specified redirect URI.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IOidcAuthenticationBuilder"/> to configure.
	/// </param>
	/// <param name="redirectUri">
	/// The redirect URI to use for OIDC authentication.
	/// </param>
	/// <returns>
	/// The <see cref="IOidcAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IOidcAuthenticationBuilder RedirectUri(
		this IOidcAuthenticationBuilder builder,
		string redirectUri)
		=> builder.ConfigureOidcClientOptions(options => options.RedirectUri = redirectUri);

	/// <summary>
	/// Let the OidcAuthenticationProvider automatically set the redirect URI to the automatically discovered one from the WebAuthenticationBroker.
	/// </summary>
	/// <remarks>
	/// This feature will use the <see cref="WebAuthenticationBroker.GetCurrentApplicationCallbackUri"/> to set the redirect URI, which is usually discovered automatically by the implementation of the WebAuthenticationBroker (which can be customized).
	/// More information: <see href="https://platform.uno/docs/articles/features/web-authentication-broker.html">Uno Web Authentication Broker Documentation</see>.
	///
	/// > ![IMPORTANT]
	/// > When this setting is set, the <see cref="OidcClientOptions.RedirectUri"/> and <see cref="OidcClientOptions.PostLogoutRedirectUri"/> settings will be overridden with the value from the WebAuthenticationBroker.
	/// </remarks>
	public static IOidcAuthenticationBuilder AutoRedirectUriFromWebAuthenticationBroker(
		this IOidcAuthenticationBuilder builder,
		bool autoReturnUri = true)
	{
		if (builder is IBuilder<OidcAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				AutoRedirectUri = autoReturnUri
			};
		}

		return builder;
	}

	/// <summary>
	/// Configures the OIDC authentication feature to be built with the specified post-logout redirect URI.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IOidcAuthenticationBuilder"/> to configure.
	/// </param>
	/// <param name="postLogoutRedirectUri">
	/// The post-logout redirect URI to use for OIDC authentication.
	/// </param>
	/// <returns>
	/// The <see cref="IOidcAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IOidcAuthenticationBuilder PostLogoutRedirectUri(
		this IOidcAuthenticationBuilder builder,
		string postLogoutRedirectUri)
		=> builder.ConfigureOidcClientOptions(options => options.PostLogoutRedirectUri = postLogoutRedirectUri);

	// Add an advanced builder allowing to tweak the options directly
	/// <summary>
	/// Configures the OIDC authentication feature by updating directly the <see cref="OidcClientOptions"/> parameter.
	/// </summary>
	/// <remarks>
	/// A
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static IOidcAuthenticationBuilder ConfigureOidcClientOptions(
		this IOidcAuthenticationBuilder builder,
		Action<OidcClientOptions> updater)
	{
		if (builder is IBuilder<OidcAuthenticationSettings> authBuilder)
		{
			if (authBuilder.Settings.Options is null)
			{
				authBuilder.Settings = authBuilder.Settings with
				{
					Options = new OidcClientOptions()
				};

			}
			updater(authBuilder.Settings.Options);
		}

		return builder;
	}
}
