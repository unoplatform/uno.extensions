namespace Uno.Extensions.Authentication;

/// <summary>
/// Extension methods for <see cref="IHandlerBuilder"/>.
/// </summary>
public static class HandlerBuilderExtensions
{
	/// <summary>
	/// Configures the handler settings to use cookies for authentication.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IHandlerBuilder"/> to use.
	/// </param>
	/// <param name="accessTokenCookie">
	/// The name of the cookie to use for the access token.
	/// </param>
	/// <param name="refreshTokenCookie">
	/// The name of the cookie to use for the refresh token. Optional
	/// </param>
	/// <returns>
	/// The <see cref="IHandlerBuilder"/> for further configuration.
	/// </returns>
	public static IHandlerBuilder Cookies(
		this IHandlerBuilder builder,
		string accessTokenCookie,
		string? refreshTokenCookie = null)
	{
		if (builder is IBuilder<HandlerSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				CookieAccessToken = accessTokenCookie,
				CookieRefreshToken = refreshTokenCookie
			};
		}

		return builder;
	}

	/// <summary>
	/// Configures the handler settings to use an authorization header for authentication.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IHandlerBuilder"/> to use.
	/// </param>
	/// <param name="scheme">
	/// The name of the scheme to use for the authorization header. This optional parameter defaults to "Bearer".
	/// </param>
	/// <returns>
	/// The <see cref="IHandlerBuilder"/> for further configuration.
	/// </returns>
	public static IHandlerBuilder AuthorizationHeader(
		this IHandlerBuilder builder,
		string? scheme = "Bearer")
	{
		if (builder is IBuilder<HandlerSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				AuthorizationHeaderScheme = scheme,
			};
		}

		return builder;
	}

	/// <summary>
	/// Configures the handler settings to use no handlers for authentication.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="IHandlerBuilder"/> to use.
	/// </param>
	/// <returns>
	/// The <see cref="IHandlerBuilder"/> for further configuration.
	/// </returns>
	public static IHandlerBuilder None(
		this IHandlerBuilder builder)
	{
		if (builder is IBuilder<HandlerSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				NoHandlers = true,
			};
		}

		return builder;
	}
}
