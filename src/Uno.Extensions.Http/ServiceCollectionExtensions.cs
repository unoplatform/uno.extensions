using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

namespace Uno.Extensions.Http
{
	public static class ServiceCollectionExtensions
	{
		private static char[] InterfaceNamePrefix = new[] { 'i', 'I' };

		public static T Conditional<T>(
			this T builder,
			bool predicate,
			Func<T, T> configureBuilder)
		{
			return (configureBuilder is not null && predicate) ? configureBuilder(builder) : builder;
		}

		public static IServiceCollection AddClient<TClient, TImplementation>(
			 this IServiceCollection services,
			 HostBuilderContext context,
			 string? name = null,
			 Func<IHttpClientBuilder, EndpointOptions, IHttpClientBuilder>? configure = null
		 )
			where TClient : class
			where TImplementation : class, TClient
		{
			Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder> httpClientFactory = (s, c) => s.AddHttpClient<TClient, TImplementation>();

			return services.AddClient<TClient>(context, name, httpClientFactory, configure);
		}

		public static IServiceCollection AddClient<TClient, TImplementation>(
			 this IServiceCollection services,
			 HostBuilderContext context,
			 EndpointOptions options,
			 string? name = null,
			 Func<IHttpClientBuilder, EndpointOptions, IHttpClientBuilder>? configure = null
		 )
			where TClient : class
			where TImplementation : class, TClient
		{
			Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder> httpClientFactory = (s, c) => s.AddHttpClient<TClient, TImplementation>();

			return services.AddClient<TClient>(context, options, name, httpClientFactory, configure);
		}

		public static IServiceCollection AddClient<TInterface>(
			 this IServiceCollection services,
			 HostBuilderContext context,
			 string? name = null,
			 Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder>? httpClientFactory = null,
			 Func<IHttpClientBuilder, EndpointOptions, IHttpClientBuilder>? configure = null
		 )
			 where TInterface : class
		{
			name ??= typeof(TInterface).IsInterface ? typeof(TInterface).Name.TrimStart(InterfaceNamePrefix) : typeof(TInterface).Name;
			var options = ConfigurationBinder.Get<EndpointOptions>(context.Configuration.GetSection(name));

			return services.AddClient<TInterface>(context, options, name, httpClientFactory, configure);
		}

		public static IServiceCollection AddClient<TInterface>(
			  this IServiceCollection services,
			  HostBuilderContext context,
			  EndpointOptions options,
			  string? name = null,
			  Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder>? httpClientFactory = null,
			  Func<IHttpClientBuilder, EndpointOptions, IHttpClientBuilder>? configure = null
		  )
			  where TInterface : class
		{
			name ??= typeof(TInterface).IsInterface ? typeof(TInterface).Name.TrimStart(InterfaceNamePrefix) : typeof(TInterface).Name;

			if (httpClientFactory is null)
			{
				httpClientFactory = (s, c) => s.AddHttpClient(name);
			}

			var httpClientBuilder = httpClientFactory(services, context);

			_ = httpClientBuilder
				.Conditional(
					options.UseNativeHandler,
					builder => builder.ConfigurePrimaryAndInnerHttpMessageHandler<HttpMessageHandler>())
				.ConfigureDelegatingHandlers()
				.ConfigureHttpClient((serviceProvider, client) =>
				{
					if (options.Url is not null)
					{
						client.BaseAddress = new Uri(options.Url);
					}
				})
				.Conditional(
					configure is not null,
					builder => configure?.Invoke(builder, options) ?? builder);
			return services;
		}


		public static IHttpClientBuilder ConfigurePrimaryAndInnerHttpMessageHandler<THandler>(this IHttpClientBuilder builder) where THandler : HttpMessageHandler
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
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

		public static IHttpClientBuilder ConfigureDelegatingHandlers(this IHttpClientBuilder builder)
		{
			if (builder == null)
			{
				throw new ArgumentNullException("builder");
			}

			builder.Services.Configure(builder.Name, delegate (HttpClientFactoryOptions options)
			{
				options.HttpMessageHandlerBuilderActions.Add(delegate (HttpMessageHandlerBuilder b)
				{
					var handlers = b.Services.GetServices<DelegatingHandler>().ToArray();
					var currentHandler = handlers.FirstOrDefault();
					if(currentHandler is not null)
					{
						for (var i = 1; i < handlers.Length; i++)
						{
							currentHandler.InnerHandler = handlers[i];
							currentHandler=handlers[i];
						}

						if(b.PrimaryHandler is not null)
						{
							currentHandler.InnerHandler = b.PrimaryHandler;
						}
						b.PrimaryHandler = handlers[0];
					}
				});
			});
			return builder;
		}

		public static IHttpClientBuilder AddTypedHttpClient<TClient>(
			this IServiceCollection services,
			Func<HttpClient, IServiceProvider, TClient> factory)
		   where TClient : class
		{
			return services
				.AddHttpClient(typeof(TClient).FullName ?? string.Empty)
				.AddTypedClient(factory);
		}

		public static IServiceCollection AddNativeHandler(this IServiceCollection services)
		{
			return services.AddTransient<HttpMessageHandler>(s =>
#if __IOS__
                new NSUrlSessionHandler()
#elif __ANDROID__
#if NET6_0_OR_GREATER
                new Xamarin.Android.Net.AndroidMessageHandler()
#else
                new Xamarin.Android.Net.AndroidClientHandler()
#endif
#elif NETFX_CORE
                new WinHttpHandler()
#else
				new HttpClientHandler()
#endif
			);
		}
	}
}
