using System.ComponentModel;

namespace Uno.Extensions.Authentication;

/// <summary>
/// Provides web-related extension methods for <see cref="IWebAuthenticationBuilder"/>.
/// </summary>
public static class WebAuthenticationBuilderExtensions
{
	/// <summary>
	/// Configures the web authentication feature to be built with a preference for ephemeral web browser sessions.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="preferEphemeral">
	/// A value indicating whether or not ephemeral web browser sessions should be preferred.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder PrefersEphemeralWebBrowserSession<TWebAuthenticationBuilder>(
	this TWebAuthenticationBuilder builder,
	bool preferEphemeral)
	where TWebAuthenticationBuilder : IWebAuthenticationBuilder
	=>
		builder.Property((WebAuthenticationSettings s)
			=> s with { PrefersEphemeralWebBrowserSession = preferEphemeral });

	/// <summary>
	/// Configures the web authentication feature to be built with the specified login start URI.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="uri">
	/// The login start URI to use.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder LoginStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string uri)
		where TWebAuthenticationBuilder : IWebAuthenticationBuilder
		=>
			builder.Property((WebAuthenticationSettings s)
				=> s with { LoginStartUri = uri });

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login start URI. 
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginStartUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate that will prepare the login start URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login start URI. This overload allows
	/// for a delegate that will use a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginStartUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate, which takes a dictionary of tokens and returns a string, that will prepare the login start URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login start URI. This overload allows
	/// for a delegate that will use a service provider for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginStartUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate, which takes a service provider and returns a string, that will prepare the login start URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login start URI. This overload allows
	/// for a delegate that will use a service provider and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginStartUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service provider, a dictionary of tokens, and returns a string—that will prepare the login start URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login start URI. This overload allows
	/// for a delegate that will use a service provider, a token cache, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginStartUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service provider, a token cache, a dictionary of tokens, and returns a string—that will prepare the login start URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder PrepareLoginStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginStartUri = prepare
				});

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login start URI. This overload allows
	/// for a delegate that will use a service of the specified type for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PrepareLoginStartUri"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the login start URI.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service of type <typeparamref name="TService"/> and returns a string—that will prepare the login start URI.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login start URI. This overload allows
	/// for a delegate that will use a service of the specified type, a service provider, a token cache reference, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PrepareLoginStartUri"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the login start URI.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service of type <typeparamref name="TService"/>, a service provider, a token cache reference, a dictionary of tokens, and returns a string—that will prepare the login start URI.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
	public static IWebAuthenticationBuilder<TService> PrepareLoginStartUri<TService>(
	this IWebAuthenticationBuilder<TService> builder,
	AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
			=> s with
			{
				PrepareLoginStartUri = prepare
			});

	/// <summary>
	/// Configures the web authentication feature to be built with the specified login callback URI.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="uri">
	/// The login callback URI to use.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder LoginCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string uri)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { LoginCallbackUri = uri });

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login callback URI.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate that will prepare the login callback URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login callback URI. This overload allows
	/// for a delegate that will use a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate, which takes a dictionary of tokens and returns a string, that will prepare the login callback URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login callback URI. This overload allows
	/// for a delegate that will use a service provider for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate, which takes a service provider and returns a string, that will prepare the login callback URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login callback URI. This overload allows
	/// for a delegate that will use a service provider and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service provider, a dictionary of tokens, and returns a string—that will prepare the login callback URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login callback URI. This overload allows
	/// for a delegate that will use a service provider, a token cache, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLoginCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service provider, a token cache, a dictionary of tokens, and returns a string—that will prepare the login callback URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder PrepareLoginCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PrepareLoginCallbackUri = prepare
				});

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login callback URI. This overload allows
	/// for a delegate that will use a service of the specified type for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PrepareLoginCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the login callback URI.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service of type <typeparamref name="TService"/> and returns a string—that will prepare the login callback URI.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the login callback URI. This overload allows
	/// for a delegate that will use a service of the specified type, a service provider, a token cache reference, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PrepareLoginCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the login callback URI.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service of type <typeparamref name="TService"/>, a service provider, a token cache reference, a dictionary of tokens, and returns a string—that will prepare the login callback URI.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
	public static IWebAuthenticationBuilder<TService> PrepareLoginCallbackUri<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with
				{
					PrepareLoginCallbackUri = prepare
				});

	/// <summary>
	/// Configures the web authentication feature to be built with the specified post login callback.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PostLoginCallback"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="postLogin">
	/// A delegate that uses a dictionary of tokens and returns a dictionary of tokens (or null) that can be used for post login operations.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder PostLogin<TWebAuthenticationBuilder>(
	this TWebAuthenticationBuilder builder,
	AsyncFunc<IDictionary<string, string>, IDictionary<string, string>?> postLogin)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

		builder.Property((WebAuthenticationSettings s)
			=> s with
			{
				PostLoginCallback = (services, cache, credentials, redirectUri, tokens, cancellationToken) =>
								postLogin(tokens, cancellationToken)
			});

	/// <summary>
	/// Configures the web authentication feature to be built with the specified post login callback. This overload allows
	/// for a delegate that will use a service provider, a token cache reference, a dictionary of credentials, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PostLoginCallback"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="postLogin">
	/// A delegate that uses a service provider, a token cache reference, a dictionary of credentials, a dictionary of tokens, and returns a dictionary of tokens (or null) that can be used for post login operations.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder PostLogin<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, IDictionary<string, string>, IDictionary<string, string>?> postLogin)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with
				{
					PostLoginCallback = (services, cache, credentials, redirectUri, tokens, cancellationToken) =>
									postLogin(services, cache, credentials, tokens, cancellationToken)
				});

	/// <summary>
	/// Configures the web authentication feature to be built with the specified post login callback. This overload allows
	/// for a delegate that will use a service of the specified type, a dictionary of tokens, and return a dictionary of tokens (or null) for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PostLoginCallback"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the return value of the post login callback.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="postLogin">
	/// A delegate—which takes a service of type <typeparamref name="TService"/>, a dictionary of tokens, and returns a dictionary of tokens (or null)—that will prepare the return value of the post login callback.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
	public static IWebAuthenticationBuilder<TService> PostLogin<TService>(
this IWebAuthenticationBuilder<TService> builder,
AsyncFunc<TService, IDictionary<string, string>, IDictionary<string, string>?> postLogin)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
		=> s with
		{
			PostLoginCallback = (service, services, cache, credentials, redirectUri, tokens, cancellationToken) =>
							postLogin(service, tokens, cancellationToken)
		});

	/// <summary>
	/// Configures the web authentication feature to be built with the specified post login callback. This overload allows
	/// for a delegate that will use a service of the specified type, a service provider, a token cache reference, a dictionary of credentials, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PostLoginCallback"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the return value of the post login callback.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="postLogin">
	/// A delegate—which takes a service of type <typeparamref name="TService"/>, a service provider, a token cache reference, a dictionary of credentials, a dictionary of tokens, and returns a dictionary of tokens (or null)—that will prepare the return value of the post login callback.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
	public static IWebAuthenticationBuilder<TService> PostLogin<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string, IDictionary<string, string>, IDictionary<string, string>?> postLogin)
			where TService : notnull =>
				builder.Property((WebAuthenticationSettings<TService> s)
					=> s with { PostLoginCallback = postLogin });

	/// <summary>
	/// Configures the web authentication feature to be built with the specified logout start URI.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.LogoutStartUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="uri">
	/// The logout start URI to use.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder LogoutStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string uri)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { LogoutStartUri = uri });

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the logout start URI.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLogoutStartUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate that will prepare the logout start URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the logout start URI. This overload allows
	/// for a delegate that will use a service provider, token cache reference, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLogoutStartUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—that takes a service provider, a token cache reference, a dictionary of tokens, and returns a string—that will prepare the logout start URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder PrepareLogoutStartUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { PrepareLogoutStartUri = prepare });

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the logout start URI. This overload allows
	/// for a delegate that will use a service of the specified type for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PrepareLogoutStartUri"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the logout start URI.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service of type <typeparamref name="TService"/> and returns a string—that will prepare the logout start URI.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the logout start URI. This overload allows
	/// for a delegate that will use a service of the specified type, a service provider, a token cache reference, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PrepareLogoutStartUri"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the logout start URI.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service of type <typeparamref name="TService"/>, a service provider, a token cache reference, a dictionary of tokens, and returns a string—that will prepare the logout start URI.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
	public static IWebAuthenticationBuilder<TService> PrepareLogoutStartUri<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with { PrepareLogoutStartUri = prepare });

	/// <summary>
	/// Configures the web authentication feature to be built with the specified logout callback URI.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.LogoutCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="uri">
	/// The logout callback URI to use.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder LogoutCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		string uri)
				where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { LogoutCallbackUri = uri });

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the logout callback URI.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLogoutCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate that will prepare the logout callback URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the logout callback URI. This overload allows
	/// for a delegate that will use a service provider, token cache reference, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.PrepareLogoutCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—that takes a service provider, a token cache reference, a dictionary of tokens, and returns a string—that will prepare the logout callback URI.
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder PrepareLogoutCallbackUri<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { PrepareLogoutCallbackUri = prepare });

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the logout callback URI. This overload allows
	/// for a delegate that will use a service of the specified type for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PrepareLogoutCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the logout callback URI.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service of type <typeparamref name="TService"/> and returns a string—that will prepare the logout callback URI.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with a delegate that will prepare the logout callback URI. This overload allows
	/// for a delegate that will use a service of the specified type, a service provider, a token cache reference, and a dictionary of tokens for authentication.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.PrepareLogoutCallbackUri"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to prepare the logout callback URI.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="prepare">
	/// A delegate—which takes a service of type <typeparamref name="TService"/>, a service provider, a token cache reference, a dictionary of tokens, and returns a string—that will prepare the logout callback URI.
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
	public static IWebAuthenticationBuilder<TService> PrepareLogoutCallbackUri<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>?, string?, string> prepare)
		where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with { PrepareLogoutCallbackUri = prepare });

	/// <summary>
	/// Configures the web authentication feature to be built with the specified refresh callback. This type of callback is used to refresh
	/// the tokens that are used for authentication when they expire.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.RefreshCallback"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="refreshCallback">
	/// A delegate that uses a dictionary of tokens and returns a dictionary of the updated tokens (or null).
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with the specified refresh callback. This type of callback is used to refresh
	/// the tokens that are used for authentication when they expire. This overload allows for a delegate that will use a service provider,
	/// a token cache reference, and a dictionary of existing tokens.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings.RefreshCallback"/>.
	/// </summary>
	/// <typeparam name="TWebAuthenticationBuilder">
	/// The type of <see cref="IWebAuthenticationBuilder"/> implementation that will be configured.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of the <see cref="IWebAuthenticationBuilder"/> implementation to configure.
	/// </param>
	/// <param name="refreshCallback">
	/// A delegate that uses a service provider, a token cache reference, a dictionary of existing tokens, and returns a dictionary of the updated tokens (or null).
	/// </param>
	/// <returns>
	/// An instance of the <see cref="IWebAuthenticationBuilder"/> implementation that was passed in.
	/// </returns>
	public static TWebAuthenticationBuilder Refresh<TWebAuthenticationBuilder>(
		this TWebAuthenticationBuilder builder,
		AsyncFunc<IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
		 		where TWebAuthenticationBuilder : IWebAuthenticationBuilder =>

			builder.Property((WebAuthenticationSettings s)
				=> s with { RefreshCallback = refreshCallback });

	/// <summary>
	/// Configures the web authentication feature to be built with the specified refresh callback. This type of callback is used to refresh
	/// the tokens that are used for authentication when they expire. This overload allows for a delegate that will use a service of the
	/// specified type and a dictionary of existing tokens.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.RefreshCallback"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to refresh the tokens.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="refreshCallback">
	/// A delegate—which takes a service of type <typeparamref name="TService"/> as well as a dictionary of existing tokens and returns a dictionary of the updated tokens (or null).
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
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

	/// <summary>
	/// Configures the web authentication feature to be built with the specified refresh callback. This type of callback is used to refresh
	/// the tokens that are used for authentication when they expire. This overload allows for a delegate that will use a service of the
	/// specified type, a service provider, a token cache reference, and a dictionary of existing tokens.
	/// The underlying property that will be set to such delegate is located on <see cref="WebAuthenticationSettings{TService}.RefreshCallback"/>.
	/// </summary>
	/// <typeparam name="TService">
	/// The type of service that will be used by the delegate to refresh the tokens.
	/// </typeparam>
	/// <param name="builder">
	/// The instance of <see cref="IWebAuthenticationBuilder{TService}"/> to configure.
	/// </param>
	/// <param name="refreshCallback">
	/// A delegate—which takes a service of type <typeparamref name="TService"/>, a service provider, a token cache reference, a dictionary of existing tokens, and returns a dictionary of the updated tokens (or null).
	/// </param>
	/// <returns>
	/// An instance of <see cref="IWebAuthenticationBuilder{TService}"/> that was passed in.
	/// </returns>
	public static IWebAuthenticationBuilder<TService> Refresh<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		AsyncFunc<TService, IServiceProvider, ITokenCache, IDictionary<string, string>, IDictionary<string, string>?> refreshCallback)
			where TService : notnull =>
			builder.Property((WebAuthenticationSettings<TService> s)
				=> s with { RefreshCallback = refreshCallback });

	// Add an advanced builder allowing to tweak the options directly
	/// <summary>
	/// Configures the Web authentication feature by updating directly the <see cref="TokenCacheOptions"/> parameter.
	/// </summary>
	/// <remarks>
	/// The <see cref="IWebAuthenticationBuilder"/> with updated <see cref="TokenCacheOptions"/> will be returned.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static IWebAuthenticationBuilder<TService> ConfigureTokenCacheKeys<TService>(
		this IWebAuthenticationBuilder<TService> builder,
		Func<TokenCacheOptionsBuilder,TokenCacheOptionsBuilder> updater) where TService : notnull
	{
		if (builder is IBuilder<WebAuthenticationSettings<TService>> authBuilder)
		{
			var optionsBuilder = TokenCacheOptionsBuilder.Create(authBuilder.Settings.TokenOptions);
			var resultingBuilder = updater.Invoke(optionsBuilder);
			authBuilder.Settings = authBuilder.Settings with
			{
				TokenOptions = resultingBuilder.Build()
			};
		}

		return builder;
	}
	// Add an advanced builder allowing to tweak the options directly
	/// <summary>
	/// Configures the Web authentication feature by updating directly the <see cref="TokenCacheOptions"/> parameter.
	/// </summary>
	/// <remarks>
	/// The <see cref="IWebAuthenticationBuilder"/> with updated <see cref="TokenCacheOptions"/> will be returned.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static IWebAuthenticationBuilder ConfigureTokenCacheKeys(
		this IWebAuthenticationBuilder builder,
		Func<TokenCacheOptionsBuilder,TokenCacheOptionsBuilder> updater)
	{
		if (builder is IBuilder<WebAuthenticationSettings> authBuilder)
		{
			var optionsBuilder = TokenCacheOptionsBuilder.Create(authBuilder.Settings.TokenOptions);
		    var resultingBuilder = updater.Invoke(optionsBuilder);
			authBuilder.Settings = authBuilder.Settings with
			{
				TokenOptions = resultingBuilder.Build()
			};
		}

		return builder;
	}
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
