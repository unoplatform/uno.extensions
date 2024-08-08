namespace Uno.Extensions.Authentication;

/// <summary>
/// Provides MSAL-related extension methods for <see cref="IMsalAuthenticationBuilder"/>.
/// </summary>
public static class MsalAuthenticationBuilderExtensions
{
	/// <summary>
	/// Configures the MSAL authentication feature to use a public client application builder.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IMsalAuthenticationBuilder"/> to configure.
	/// </param>
	/// <param name="build">
	/// A delegate which can be used to create the public client application builder and configure MsalAuthenticationSettings to use it.
	/// </param>
	/// <returns>
	/// The <see cref="IMsalAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IMsalAuthenticationBuilder Builder(
		this IMsalAuthenticationBuilder builder,
		Action<PublicClientApplicationBuilder> build
		)
	{
#if !UNO_EXT_MSAL
		return builder;
#else
		if (builder is IBuilder<MsalAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				Build = build
			};
		}

		return builder;
#endif
	}

	/// <summary>
	/// Configures the MSAL authentication feature to use an incremental builder for StorageCreationProperties objects.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IMsalAuthenticationBuilder"/> to configure.
	/// </param>
	/// <param name="store">
	/// A delegate which can be used to create the StorageCreationPropertiesBuilder and configure MsalAuthenticationSettings to use it.
	/// </param>
	/// <returns>
	/// The <see cref="IMsalAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IMsalAuthenticationBuilder Storage(
		this IMsalAuthenticationBuilder builder,
		Action<StorageCreationPropertiesBuilder> store
		)
	{
#if !UNO_EXT_MSAL
		return builder;
#else
		if (builder is IBuilder<MsalAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				Store = store
			};
		}

		return builder;
#endif
	}

	/// <summary>
	/// Configures the MSAL authentication feature to be built with the specified scopes for authentication.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IMsalAuthenticationBuilder"/> to configure.
	/// </param>
	/// <param name="scopes">
	/// The MSAL scopes to use for authentication.
	/// </param>
	/// <returns>
	/// The <see cref="IMsalAuthenticationBuilder"/> that was passed in.
	/// </returns>
	public static IMsalAuthenticationBuilder Scopes(
		this IMsalAuthenticationBuilder builder,
		string[] scopes
		)
	{
#if !UNO_EXT_MSAL
		return builder;
#else
		if (builder is IBuilder<MsalAuthenticationSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				Scopes = scopes
			};
		}

		return builder;
#endif
	}

	/// <summary>
	/// Configures a public client application builder to create the MSAL authentication 
	/// feature to use the redirect Uri provided by WebAuthenticationBroker.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="PublicClientApplicationBuilder"/> to configure.
	/// </param>
	/// <returns>
	/// The <see cref="PublicClientApplicationBuilder"/> that was passed in.
	/// </returns>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static PublicClientApplicationBuilder WithWebRedirectUri(this PublicClientApplicationBuilder builder)
	{
#if !UNO_EXT_MSAL
		return builder;
#else
		return builder.WithRedirectUri(WebAuthenticationBroker.GetCurrentApplicationCallbackUri().OriginalString);
#endif
	}
}
