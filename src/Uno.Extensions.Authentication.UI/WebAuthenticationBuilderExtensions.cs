namespace Uno.Extensions.Authentication;

public static class WebAuthenticationBuilderExtensions
{
	public static TWebAuthenticationBuilder PrefersEphemeralWebBrowserSession<TWebAuthenticationBuilder>(
	this TWebAuthenticationBuilder builder,
	bool preferEphemeral)
	where TWebAuthenticationBuilder : IWebAuthenticationBuilder
	=>
		builder.Property((WebAuthenticationSettings s)
			=> s with { PrefersEphemeralWebBrowserSession = preferEphemeral });

	public static TWebAuthenticationBuilder LoginStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string uri)
		where TWebAuthenticationBuilder: IWebAuthenticationBuilder
		=>
			builder.Property((WebAuthenticationSettings s)
				=> s with { LoginStartUri = uri });

	public static TWebAuthenticationBuilder PrepareLoginStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<string> prepare)
		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>
			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginStartUri = (services, cache, tokens, loginStartUri, cancellationToken) =>
									prepare(cancellationToken)
				});


	public static TWebAuthenticationBuilder PrepareLoginStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IDictionary<string, string>?, string> prepare)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginStartUri = (services, cache, tokens, loginStartUri, cancellationToken) =>
									prepare(tokens, cancellationToken)
				});

	public static TWebAuthenticationBuilder PrepareLoginStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, string> prepare)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginStartUri = (services, cache, tokens, loginStartUri, cancellationToken) =>
									prepare(services, cancellationToken)
				});

	public static TWebAuthenticationBuilder PrepareLoginStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDictionary<string, string>?, string> prepare)
		   		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginStartUri = (services, cache, tokens, loginStartUri, cancellationToken) =>
									prepare(services, tokens, cancellationToken)
				});

	public static TWebAuthenticationBuilder PrepareLoginStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginStartUri = prepare
				});

	public static IWebAuthenticationBuilder<TService> PrepareLoginStartUri<TService>(
	this IWebAuthenticationBuilder<TService> builder,
	AsyncFunc<TService, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
			=> s with
			{
				PrepareLoginStartUri = (service, services, cache, tokens, loginStartUri, cancellationToken) =>
								prepare(service, cancellationToken)
			});

	public static IWebAuthenticationBuilder<TService> PrepareLoginStartUri<TService>(
	this IWebAuthenticationBuilder<TService> builder,
	AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
			=> s with
			{
				PrepareLoginStartUri = prepare
			});

	public static TWebAuthenticationBuilder LoginCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string uri)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { LoginCallbackUri = uri });

	public static TWebAuthenticationBuilder PrepareLoginCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<string> prepare)
		   		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginCallbackUri = (services, cache, tokens, loginCallbackUri, cancellationToken) =>
									prepare(cancellationToken)
				});

	public static TWebAuthenticationBuilder PrepareLoginCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IDictionary<string, string>?, string> prepare)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginCallbackUri = (services, cache, tokens, loginCallbackUri, cancellationToken) =>
									prepare(tokens, cancellationToken)
				});

	public static TWebAuthenticationBuilder PrepareLoginCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, string> prepare)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginCallbackUri = (services, cache, tokens, loginCallbackUri, cancellationToken) =>
									prepare(services, cancellationToken)
				});

	public static TWebAuthenticationBuilder PrepareLoginCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, IDictionary<string, string>?, string?, string> prepare)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginCallbackUri = (services, cache, tokens, loginCallbackUri, cancellationToken) =>
									prepare(services, tokens, loginCallbackUri, cancellationToken)
				});

	public static TWebAuthenticationBuilder PrepareLoginCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginCallbackUri = prepare
				});

	public static IWebAuthenticationBuilder<TService> PrepareLoginCallbackUri<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with
				{
					PrepareLoginCallbackUri = (service, services, cache, tokens, loginCallbackUri, cancellationToken) =>
									prepare(service, cancellationToken)
				});

	public static IWebAuthenticationBuilder<TService> PrepareLoginCallbackUri<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with
				{
					PrepareLoginCallbackUri = prepare
				});

	public static TWebAuthenticationBuilder PostLogin<TWebAuthenticationBuilder>(
	this TWebAuthenticationBuilder builder,
	AsyncFunc<IDictionary<string, string>, IDictionary<string, string>?> postLogin)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

		builder.Property((WebAuthenticationSettings s)
			=> s with
			{
				PostLoginCallback = (services, cache, credentials, tokens, cancellationToken) =>
								postLogin(tokens, cancellationToken)
			});


	public static TWebAuthenticationBuilder PostLogin<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, IDictionary<string, string>, IDictionary<string, string>?> postLogin)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PostLoginCallback = (services, cache, credentials, tokens, cancellationToken) =>
									postLogin(services, cache, credentials, tokens, cancellationToken)
				});


	public static IWebAuthenticationBuilder<TService> PostLogin<TService>(
this IWebAuthenticationBuilder<TService> builder,
AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?> postLogin)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
		=> s with
		{
			PostLoginCallback = (service, services, cache, credentials, tokens, cancellationToken) =>
							postLogin(service, tokens, cancellationToken)
		});


	public static IWebAuthenticationBuilder<TService> PostLogin<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, IDictionary<string, string>, IDictionary<string, string>?> postLogin)
			where TService : notnull =>
				builder.Property((WebAuthenticationSettings<TService> s)
					=> s with { PostLoginCallback = postLogin });




	public static TWebAuthenticationBuilder LogoutStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string uri)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { LogoutStartUri = uri });

	public static TWebAuthenticationBuilder PrepareLogoutStartUri<TWebAuthenticationBuilder>(
	this TWebAuthenticationBuilder builder,
	AsyncFunc<string> prepare)
		   		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

		builder.Property((WebAuthenticationSettings s)
			=> s with
			{
				PrepareLogoutStartUri = (services, cache, tokens, logoutStartUri, cancellationToken) =>
									prepare(cancellationToken)
			});

	public static TWebAuthenticationBuilder PrepareLogoutStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { PrepareLogoutStartUri = prepare });

	public static IWebAuthenticationBuilder<TService> PrepareLogoutStartUri<TService>(
	this IWebAuthenticationBuilder<TService> builder,
	AsyncFunc<TService, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
			=> s with
			{
				PrepareLogoutStartUri = (service, services, cache, tokens, logoutStartUri, cancellationToken) =>
									prepare(service, cancellationToken)
			});

	public static IWebAuthenticationBuilder<TService> PrepareLogoutStartUri<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with { PrepareLogoutStartUri = prepare });


	public static TWebAuthenticationBuilder LogoutCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string uri)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { LogoutCallbackUri = uri });

	public static TWebAuthenticationBuilder PrepareLogoutCallbackUri<TWebAuthenticationBuilder>(
	this TWebAuthenticationBuilder builder,
	AsyncFunc<string> prepare)
		   		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

		builder.Property((WebAuthenticationSettings s)
			=> s with
			{
				PrepareLogoutCallbackUri = (services, cache, tokens, loginCallbackUri, cancellationToken) =>
									prepare(cancellationToken)
			});


	public static TWebAuthenticationBuilder PrepareLogoutCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { PrepareLogoutCallbackUri = prepare });

	public static IWebAuthenticationBuilder<TService> PrepareLogoutCallbackUri<TService>(
this IWebAuthenticationBuilder<TService> builder,
AsyncFunc<TService, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
		=> s with
		{
			PrepareLogoutCallbackUri = (service, services, cache, tokens, loginCallbackUri, cancellationToken) =>
								prepare(service, cancellationToken)
		});


	public static IWebAuthenticationBuilder<TService> PrepareLogoutCallbackUri<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with { PrepareLogoutCallbackUri = prepare });

	public static TWebAuthenticationBuilder Refresh<TWebAuthenticationBuilder>(
	this TWebAuthenticationBuilder builder,
	AsyncFunc<IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
		  		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

		builder.Property((WebAuthenticationSettings s)
			=> s with
			{
				RefreshCallback = (services, cache, tokens, cancellationToken) =>
			refreshCallback(tokens, cancellationToken)
			});


	public static TWebAuthenticationBuilder Refresh<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { RefreshCallback = refreshCallback });

	public static IWebAuthenticationBuilder<TService> Refresh<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
			where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with
				{
					RefreshCallback = (service, services, cache, tokens, cancellationToken) =>
			refreshCallback(service, tokens, cancellationToken)
				});


	public static IWebAuthenticationBuilder<TService> Refresh<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
			where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with { RefreshCallback = refreshCallback });


	public static TWebAuthenticationBuilder AccessTokenKey<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string key)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { AccessTokenKey = key });

	public static TWebAuthenticationBuilder RefreshTokenKey<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string key)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { RefreshTokenKey = key });

	public static TWebAuthenticationBuilder OtherTokenKeys<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		IDictionary<string, string> keys)
		  		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

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
