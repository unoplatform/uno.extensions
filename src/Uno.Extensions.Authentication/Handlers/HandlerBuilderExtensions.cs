namespace Uno.Extensions.Authentication.Handlers;

public static class HandlerBuilderExtensions
{
	public static IHandlerBuilder WithCookies(
		this IHandlerBuilder builder,
		string accessTokenCookie,
		string? refreshTokenCookie=null)
	{
		if (builder is IBuilder<HandlerSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				CookieAccessToken= accessTokenCookie,
				CookieRefreshToken= refreshTokenCookie
			};
		}

		return builder;
	}

	public static IHandlerBuilder WithAuthorizationHeader(
		this IHandlerBuilder builder,
		string? scheme = "Bearer")
	{
		if (builder is IBuilder<HandlerSettings> authBuilder)
		{
			authBuilder.Settings = authBuilder.Settings with
			{
				AuthorizationHeaderScheme= scheme,
			};
		}

		return builder;
	}
}
