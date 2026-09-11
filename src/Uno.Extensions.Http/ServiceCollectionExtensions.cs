using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
	internal const string RequiresDynamicCodeMessage = "Binding strongly typed objects to configuration values may require generating dynamic code at runtime. [From Array.CreateInstance() and others.]";
	internal const string RequiresUnreferencedCodeMessage = "Cannot statically analyze the type of instance so its members may be trimmed. [From TypeDescriptor.GetConverter() and others.]";

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
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IServiceCollection AddClient<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TClient,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TImplementation
	>(
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
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IServiceCollection AddClientWithEndpoint<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TClient,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TImplementation,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		TEndpoint
	>(
		 this IServiceCollection services,
		 HostBuilderContext context,
		 TEndpoint? options = null,
		 string? name = null,
		 Func<IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder>? configure = null
	 )
		where TClient : class
		where TImplementation : class, TClient
		where TEndpoint : EndpointOptions, new()
		=> services.AddClientWithEndpoint<TClient, TEndpoint>(context, options, name, TypedClientFactory<TClient, TImplementation>(name), configure);

	/// <summary>
	/// The typed-client registration shared by the transient overloads: <c>AddHttpClient</c>'s own
	/// (transient) typed registration, named when a name is supplied.
	/// </summary>
	private static Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder> TypedClientFactory<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TClient,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TImplementation
	>(string? name)
		where TClient : class
		where TImplementation : class, TClient
		=> (s, c) => (name is null || string.IsNullOrWhiteSpace(name)) ?
						s.AddHttpClient<TClient, TImplementation>() :
						s.AddHttpClient<TClient, TImplementation>(name);

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
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
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
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IServiceCollection AddClientWithEndpoint<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TInterface,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		TEndpoint
	>(
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
	/// Adds a typed client to the service collection with the specified service lifetime.
	/// </summary>
	/// <typeparam name="TClient">The type of client to add</typeparam>
	/// <typeparam name="TImplementation">The type implementation</typeparam>
	/// <param name="services">The service collection to register with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="lifetime">
	/// The service lifetime to register the client with. <see cref="ServiceLifetime.Transient"/>
	/// behaves exactly like the overloads without a lifetime. A non-transient client captures its
	/// <see cref="HttpClient"/> for the registration's lifetime, forgoing the factory's handler
	/// rotation (stale-DNS mitigation) - which is why typed clients are transient by default.
	/// Choose it when the client has to carry state across resolutions.
	/// </param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional] Name of the endpoint (used to load from appsettings)</param>
	/// <param name="configure">[optional] Callback to configure the endpoint</param>
	/// <returns>Updated service collection</returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IServiceCollection AddClient<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TClient,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TImplementation
	>(
		 this IServiceCollection services,
		 HostBuilderContext context,
		 ServiceLifetime lifetime,
		 EndpointOptions? options = null,
		 string? name = null,
		 Func<IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder>? configure = null
	 )
		where TClient : class
		where TImplementation : class, TClient
		=> services.AddClientWithEndpoint<TClient, TImplementation, EndpointOptions>(context, lifetime, options, name, configure);

	/// <summary>
	/// Adds a typed client to the service collection with the specified service lifetime.
	/// </summary>
	/// <typeparam name="TClient">The type of client to add</typeparam>
	/// <typeparam name="TImplementation">The type implementation</typeparam>
	/// <typeparam name="TEndpoint">The type of endpoint to register</typeparam>
	/// <param name="services">The service collection to register with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="lifetime">
	/// The service lifetime to register the client with; see
	/// <see cref="AddClient{TClient, TImplementation}(IServiceCollection, HostBuilderContext, ServiceLifetime, EndpointOptions?, string?, Func{IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder}?)"/>.
	/// </param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional] Name of the endpoint (used to load from appsettings)</param>
	/// <param name="configure">[optional] Callback to configure the endpoint</param>
	/// <returns>Updated service collection</returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IServiceCollection AddClientWithEndpoint<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TClient,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TImplementation,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		TEndpoint
	>(
		 this IServiceCollection services,
		 HostBuilderContext context,
		 ServiceLifetime lifetime,
		 TEndpoint? options = null,
		 string? name = null,
		 Func<IHttpClientBuilder, TEndpoint?, IHttpClientBuilder>? configure = null
	 )
		where TClient : class
		where TImplementation : class, TClient
		where TEndpoint : EndpointOptions, new()
	{
		if (lifetime == ServiceLifetime.Transient)
		{
			// Same typed (transient) registration as the lifetime-less overload; the configure
			// callback stays typed to TEndpoint.
			return services.AddClientWithEndpoint<TClient, TEndpoint>(context, options, name, TypedClientFactory<TClient, TImplementation>(name), configure);
		}

		// Non-transient: register the endpoint pipeline as a NAMED client (base address, native
		// handler, delegating handlers, configure callback), then register the client type with
		// the requested lifetime, built through ITypedHttpClientFactory - the supported way to
		// construct typed clients outside AddHttpClient<T>'s own (transient) registration.
		var endpointName = EndpointNameForType<TClient>(name);

		services.AddClientWithEndpoint<TClient, TEndpoint>(
			context,
			options,
			endpointName,
			httpClientFactory: (s, c) => s.AddHttpClient(endpointName),
			configure);

		services.Add(ServiceDescriptor.Describe(
			typeof(TClient),
			sp => sp.GetRequiredService<ITypedHttpClientFactory<TImplementation>>()
					.CreateClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(endpointName)),
			lifetime));

		return services;
	}

	/// <summary>
	/// Adds a typed client to the service collection with the specified service lifetime.
	/// </summary>
	/// <typeparam name="TInterface">The type of client to add</typeparam>
	/// <param name="services">The service collection to register with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="lifetime">
	/// The service lifetime to register the client with; see
	/// <see cref="AddClient{TClient, TImplementation}(IServiceCollection, HostBuilderContext, ServiceLifetime, EndpointOptions?, string?, Func{IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder}?)"/>.
	/// </param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional] Name of the endpoint (used to load from appsettings)</param>
	/// <param name="configure">[optional] Callback to configure the endpoint</param>
	/// <returns>Updated service collection</returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IServiceCollection AddClient<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TInterface
	>(
		  this IServiceCollection services,
		  HostBuilderContext context,
		  ServiceLifetime lifetime,
		  EndpointOptions? options = null,
		  string? name = null,
		  Func<IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder>? configure = null
	  )
		  where TInterface : class
		=> services.AddClientWithEndpoint<TInterface, EndpointOptions>(context, lifetime, options, name, configure);

	/// <summary>
	/// Adds a typed client to the service collection with the specified service lifetime.
	/// </summary>
	/// <typeparam name="TInterface">The type of client to add</typeparam>
	/// <typeparam name="TEndpoint">The type of endpoint to register</typeparam>
	/// <param name="services">The service collection to register with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="lifetime">
	/// The service lifetime to register the client with; see
	/// <see cref="AddClient{TClient, TImplementation}(IServiceCollection, HostBuilderContext, ServiceLifetime, EndpointOptions?, string?, Func{IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder}?)"/>.
	/// </param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional] Name of the endpoint (used to load from appsettings)</param>
	/// <param name="configure">[optional] Callback to configure the endpoint</param>
	/// <returns>Updated service collection</returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IServiceCollection AddClientWithEndpoint<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		TInterface,
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		TEndpoint
	>(
		  this IServiceCollection services,
		  HostBuilderContext context,
		  ServiceLifetime lifetime,
		  TEndpoint? options = null,
		  string? name = null,
		  Func<IHttpClientBuilder, TEndpoint?, IHttpClientBuilder>? configure = null
	  )
		  where TInterface : class
		where TEndpoint : EndpointOptions, new()
	{
		if (typeof(TInterface).IsInterface)
		{
			// The single-generic shape registers the type as its own implementation, which for an
			// interface only fails at the first resolve, far from the registration that caused it.
			throw new ArgumentException(
				$"{typeof(TInterface).Name} is an interface and cannot be built as its own implementation. Use the AddClient<TInterface, TImplementation>(..., lifetime, ...) overload.",
				nameof(TInterface));
		}

		return services.AddClientWithEndpoint<TInterface, TInterface, TEndpoint>(context, lifetime, options, name, configure);
	}

	/// <summary>
	/// The endpoint name used when none is supplied: the client's type name, with a leading 'I'
	/// stripped for interfaces - matching how the transient overloads resolve their configuration
	/// section.
	/// </summary>
	private static string EndpointNameForType<TClient>(string? name) =>
		(name is not null && !string.IsNullOrWhiteSpace(name))
			? name
			: (typeof(TClient).IsInterface ? typeof(TClient).Name.TrimStart(InterfaceNamePrefix) : typeof(TClient).Name);

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
