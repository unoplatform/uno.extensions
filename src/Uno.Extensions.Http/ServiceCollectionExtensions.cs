namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
	private static char[] InterfaceNamePrefix = new[] { 'i', 'I' };

	private static T Conditional<T>(
		this T builder,
		bool predicate,
		Func<T, T> configureBuilder)
	{
		return (configureBuilder is not null && predicate) ? configureBuilder(builder) : builder;
	}

	/// <summary>
	/// Adds a typed client to the service collection.
	/// </summary>
	/// <typeparam name="TClient">The type of client to add</typeparam>
	/// <typeparam name="TImplementation">The type implementation</typeparam>
	/// <param name="services">The service collection to register with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional] Name of the endpoint (used to load from appsettings)</param>
	/// <param name="configure">[optional] Callback to configure the endpoint</param>
	/// <returns>Updated service collection</returns>
	public static IServiceCollection AddClient<TClient, TImplementation>(
		 this IServiceCollection services,
		 HostBuilderContext context,
		 EndpointOptions? options = null,
		 string? name = null,
		 Func<IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder>? configure = null
	 )
		where TClient : class
		where TImplementation : class, TClient
		=> services.AddClientWithEndpoint<TClient,TImplementation,EndpointOptions>(context, options, name, configure);

	/// <summary>
	/// Adds a typed client to the service collection.
	/// </summary>
	/// <typeparam name="TClient">The type of client to add</typeparam>
	/// <typeparam name="TImplementation">The type implementation</typeparam>
	/// <typeparam name="TEndpoint">The type of endpoint to register</typeparam>
	/// <param name="services">The service collection to register with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional] Name of the endpoint (used to load from appsettings)</param>
	/// <param name="configure">[optional] Callback to configure the endpoint</param>
	/// <returns>Updated service collection</returns>
	public static IServiceCollection AddClientWithEndpoint<TClient, TImplementation, TEndpoint>(
		 this IServiceCollection services,
		 HostBuilderContext context,
		 TEndpoint? options = null,
		 string? name = null,
		 Func<IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder>? configure = null
	 )
		where TClient : class
		where TImplementation : class, TClient
		where TEndpoint : EndpointOptions, new()
	{
		Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder> httpClientFactory =
			(s, c) => (name is null || string.IsNullOrWhiteSpace(name)) ?
						s.AddHttpClient<TClient, TImplementation>() :
						s.AddHttpClient<TClient, TImplementation>(name);

		return services.AddClientWithEndpoint<TClient, TEndpoint>(context, options, name, httpClientFactory, configure);
	}

	/// <summary>
	/// Adds a typed client to the service collection.
	/// </summary>
	/// <typeparam name="TInterface">The type of client to add</typeparam>
	/// <param name="services">The service collection to register with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional] Name of the endpoint (used to load from appsettings)</param>
	/// <param name="httpClientFactory">[optional] Callback to configure the HttpClient</param>
	/// <param name="configure">[optional] Callback to configure the endpoint</param>
	/// <returns>Updated service collection</returns>
	public static IServiceCollection AddClient<TInterface>(
		  this IServiceCollection services,
		  HostBuilderContext context,
		  EndpointOptions? options = null,
		  string? name = null,
		  Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder>? httpClientFactory = null,
		  Func<IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder>? configure = null
	  )
		  where TInterface : class
		=> services.AddClientWithEndpoint<TInterface, EndpointOptions>(context, options, name, httpClientFactory, configure);

	/// <summary>
	/// Adds a typed client to the service collection.
	/// </summary>
	/// <typeparam name="TInterface">The type of client to add</typeparam>
	/// <typeparam name="TEndpoint">The type of endpoint to register</typeparam>
	/// <param name="services">The service collection to register with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional] Name of the endpoint (used to load from appsettings)</param>
	/// <param name="httpClientFactory">[optional] Callback to configure the HttpClient</param>
	/// <param name="configure">[optional] Callback to configure the endpoint</param>
	/// <returns>Updated service collection</returns>
	public static IServiceCollection AddClientWithEndpoint<TInterface, TEndpoint>(
		  this IServiceCollection services,
		  HostBuilderContext context,
		  TEndpoint? options = null,
		  string? name = null,
		  Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder>? httpClientFactory = null,
		  Func<IHttpClientBuilder, TEndpoint?, IHttpClientBuilder>? configure = null
	  )
		  where TInterface : class
		where TEndpoint : EndpointOptions, new()
	{
		var optionsName = name ?? (typeof(TInterface).IsInterface ? typeof(TInterface).Name.TrimStart(InterfaceNamePrefix) : typeof(TInterface).Name);
		options ??= ConfigurationBinder.Get<TEndpoint>(context.Configuration.GetSection(optionsName));

		httpClientFactory ??=
			(s, c) => (name is null || string.IsNullOrWhiteSpace(name)) ?
						s.AddHttpClient<TInterface>() :
						s.AddHttpClient<TInterface>(name);

		var httpClientBuilder = httpClientFactory(services, context);

		_ = httpClientBuilder
			.Conditional(
				options?.UseNativeHandler ?? true,
				builder => builder.ConfigurePrimaryAndInnerHttpMessageHandler<HttpMessageHandler>())
			.ConfigureDelegatingHandlers()
			.ConfigureHttpClient((serviceProvider, client) =>
			{
				if (options?.Url is not null)
				{
					client.BaseAddress = new Uri(options.Url);
				}
			})
			.Conditional(
				configure is not null,
				builder => configure?.Invoke(builder, options) ?? builder);
		return services;
	}

	/// <summary>
	/// Configures the primary and inner http message handler.
	/// </summary>
	/// <typeparam name="THandler">The type to register as the primary message handler</typeparam>
	/// <param name="builder">The client builder to configure</param>
	/// <returns>The configured client builder</returns>
	/// <exception cref="ArgumentNullException">builder parameter can't be null</exception>
	public static IHttpClientBuilder ConfigurePrimaryAndInnerHttpMessageHandler<THandler>(this IHttpClientBuilder builder) where THandler : HttpMessageHandler
	{
		if (builder == null)
		{
			throw new ArgumentNullException(nameof(builder));
		}

		builder.Services.Configure(builder.Name, delegate (HttpClientFactoryOptions options)
		{
			options.HttpMessageHandlerBuilderActions.Add(delegate (HttpMessageHandlerBuilder b)
			{
				var innerHandler = b.Services.GetRequiredService<THandler>() as HttpMessageHandler;
				if (b.PrimaryHandler is DelegatingHandler delegatingHandler)
				{
					if (delegatingHandler.InnerHandler is not null &&
							innerHandler is DelegatingHandler innerDelegating)
					{
						innerDelegating.InnerHandler = delegatingHandler.InnerHandler;
					}
					delegatingHandler.InnerHandler = innerHandler;
					innerHandler = delegatingHandler;
				}

				b.PrimaryHandler = innerHandler;
			});
		});
		return builder;
	}

	/// <summary>
	/// Configure the delegating handlers.
	/// </summary>
	/// <param name="builder">The client builder to configure</param>
	/// <returns>Configured client builder</returns>
	/// <exception cref="ArgumentNullException">builder parameter can't be null</exception>
	public static IHttpClientBuilder ConfigureDelegatingHandlers(this IHttpClientBuilder builder)
	{
		if (builder == null)
		{
			throw new ArgumentNullException(nameof(builder));
		}

		builder.Services.Configure(builder.Name, delegate (HttpClientFactoryOptions options)
		{
			options.HttpMessageHandlerBuilderActions.Add(delegate (HttpMessageHandlerBuilder b)
			{
				var handlers = b.Services.GetServices<DelegatingHandler>().ToArray();
				var currentHandler = handlers.FirstOrDefault();
				if (currentHandler is not null)
				{
					for (var i = 1; i < handlers.Length; i++)
					{
						currentHandler.InnerHandler = handlers[i];
						currentHandler = handlers[i];
					}

					if (b.PrimaryHandler is not null)
					{
						currentHandler.InnerHandler = b.PrimaryHandler;
					}
					b.PrimaryHandler = handlers[0];
				}
			});
		});
		return builder;
	}

	/// <summary>
	/// Registered a typed http client
	/// </summary>
	/// <typeparam name="TClient">The type to register</typeparam>
	/// <param name="services">The service collection to register with</param>
	/// <param name="factory">The factory to create the http client</param>
	/// <returns>Configured client builder</returns>
	public static IHttpClientBuilder AddTypedHttpClient<TClient>(
		this IServiceCollection services,
		Func<HttpClient, IServiceProvider, TClient> factory)
	   where TClient : class
	{
		return services
			.AddHttpClient(typeof(TClient).FullName ?? string.Empty)
			.AddTypedClient(factory);
	}
}
