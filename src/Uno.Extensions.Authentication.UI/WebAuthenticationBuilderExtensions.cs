using System;

namespace Uno.Extensions.Authentication;

public static class WebAuthenticationBuilderExtensions
{
	public static IWebAuthenticationBuilder LoginStartUri(
		this IWebAuthenticationBuilder builder,
		string uri) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { LoginStartUri = uri });

	public static IWebAuthenticationBuilder PrepareLoginStartUri(
		this IWebAuthenticationBuilder builder,
		AsyncFunc<string?, IDictionary<string, string>?, string> prepare) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { PrepareLoginStartUri = prepare });


	public static IWebAuthenticationBuilder LoginCallbackUri(
		this IWebAuthenticationBuilder builder,
		string uri) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { LoginCallbackUri = uri });

	public static IWebAuthenticationBuilder PrepareLoginCallbackUri(
		this IWebAuthenticationBuilder builder,
		AsyncFunc<string?, IDictionary<string, string>?, string> prepare) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { PrepareLoginCallbackUri = prepare });

	public static IWebAuthenticationBuilder PostLogin(
		this IWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDictionary<string, string>?, IDictionary<string, string>, IDictionary<string, string>?> postLogin) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { PostLoginCallback = postLogin });

	public static IWebAuthenticationBuilder PostLogin(
		this IWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDictionary<string, string>, IDictionary<string, string>?> postLogin) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PostLoginCallback =
						postLogin is not null ?
							(services, credentials, tokens, ct) => postLogin(services, tokens, ct) :
							default
				});

	public static IWebAuthenticationBuilder PostLogin<TService>(
		this IWebAuthenticationBuilder builder,
		AsyncFunc<TService, IDictionary<string, string>?, IDictionary<string, string>, IDictionary<string, string>?> postLogin)
			where TService : notnull =>
				builder.Property((WebAuthenticationSettings<TService> s)
					=> s with { PostLoginCallback = postLogin });

	public static IWebAuthenticationBuilder PostLogin<TService>(
	this IWebAuthenticationBuilder builder,
	AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?> postLogin)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with
				{
					PostLoginCallback =
						postLogin is not null ?
							(service, credentials, tokens, ct) => postLogin(service, tokens, ct) :
							default
				});

	public static IWebAuthenticationBuilder LogoutStartUri(
		this IWebAuthenticationBuilder builder,
		string uri) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { LogoutStartUri = uri });

	public static IWebAuthenticationBuilder PrepareLogoutStartUri(
		this IWebAuthenticationBuilder builder,
		AsyncFunc<string?, IDictionary<string, string>?, string> prepare) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { PrepareLogoutStartUri = prepare });

	public static IWebAuthenticationBuilder LogoutCallbackUri(
		this IWebAuthenticationBuilder builder,
		string uri) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { LogoutCallbackUri = uri });

	public static IWebAuthenticationBuilder PrepareLogoutCallbackUri(
		this IWebAuthenticationBuilder builder,
		AsyncFunc<string?, IDictionary<string, string>?, string> prepare) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { PrepareLogoutCallbackUri = prepare });

	public static IWebAuthenticationBuilder Refresh(
		this IWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { RefreshCallback = refreshCallback });


	public static IWebAuthenticationBuilder Refresh<TService>(
		this IWebAuthenticationBuilder builder,
		AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
			where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with { RefreshCallback = refreshCallback });


	public static IWebAuthenticationBuilder AccessTokenKey(
		this IWebAuthenticationBuilder builder,
		string key) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { AccessTokenKey = key });

	public static IWebAuthenticationBuilder RefreshTokenKey(
		this IWebAuthenticationBuilder builder,
		string key) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { RefreshTokenKey = key });

	public static IWebAuthenticationBuilder OtherTokenKeys(
		this IWebAuthenticationBuilder builder,
		IDictionary<string, string> keys) =>
			builder.Property((WebAuthenticationSettings s)
				=> s with { OtherTokenKeys = keys });

	private static TBuilder Property<TBuilder, TSettings>(
		this TBuilder builder,
		Func<TSettings, TSettings> setProperty)
		where TBuilder : IBuilder
		where TSettings : new()
	{
		if (builder is IBuilder<TSettings> authBuilder)
		{
			authBuilder.Settings = setProperty(authBuilder.Settings);
		}

		return builder;
	}
}
