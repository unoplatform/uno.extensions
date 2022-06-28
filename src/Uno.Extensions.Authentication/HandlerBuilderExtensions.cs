namespace Uno.Extensions.Authentication;

public static class HandlerBuilderExtensions
{
	public static IHandlerBuilder Cookies(
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

	public static IHandlerBuilder AuthorizationHeader(
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
