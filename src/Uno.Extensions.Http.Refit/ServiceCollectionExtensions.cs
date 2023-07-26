using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using Uno.Extensions.Http;

namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers a Refit client with the specified <paramref name="name"/>.
	/// </summary>
	/// <typeparam name="TInterface">The Refit api type to register</typeparam>
	/// <param name="services">The services collection to register the api with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional]The name for locating endpoint information in appsettings</param>
	/// <param name="settingsBuilder">[optional]Callback for overriding Refit settings</param>
	/// <param name="configure">[optional]Callback for configuring the endpoint</param>
	/// <returns></returns>
	public static IServiceCollection AddRefitClient<TInterface>(
		   this IServiceCollection services,
		   HostBuilderContext context,
		   EndpointOptions? options = null,
		   string? name = null,
		   Action<IServiceProvider, RefitSettings>? settingsBuilder = null,
		   Func<IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder>? configure = null
	   )
		   where TInterface : class
		=> services.AddRefitClientWithEndpoint<TInterface, EndpointOptions>(context, options, name, settingsBuilder, configure);

	/// <summary>
	/// Registers a Refit client with the specified <paramref name="name"/>.
	/// </summary>
	/// <typeparam name="TInterface">The Refit api type to register</typeparam>
	/// <typeparam name="TEndpoint">The type of endpoint to register</typeparam>
	/// <param name="services">The services collection to register the api with</param>
	/// <param name="context">The host builder context</param>
	/// <param name="options">[optional] Endpoint information (loaded from appsettings if not specified)</param>
	/// <param name="name">[optional]The name for locating endpoint information in appsettings</param>
	/// <param name="settingsBuilder">[optional]Callback for overriding Refit settings</param>
	/// <param name="configure">[optional]Callback for configuring the endpoint</param>
	/// <returns></returns>
	public static IServiceCollection AddRefitClientWithEndpoint<TInterface, TEndpoint>(
		   this IServiceCollection services,
		   HostBuilderContext context,
		   TEndpoint? options = null,
		   string? name = null,
		   Action<IServiceProvider, RefitSettings>? settingsBuilder = null,
		   Func<IHttpClientBuilder, TEndpoint?, IHttpClientBuilder>? configure = null
	   )
		   where TInterface : class
		where TEndpoint : EndpointOptions, new()
	{
		return services.AddClientWithEndpoint<TInterface, TEndpoint>(
			context,
			options,
			name: name,
			httpClientFactory: (s, c) => Refit.HttpClientFactoryExtensions.AddRefitClient<TInterface>(s, settingsAction: serviceProvider =>
			{
				var serializer = serviceProvider.GetService<IHttpContentSerializer>();
				var settings = serializer is not null ? new RefitSettings() { ContentSerializer = serializer } : new RefitSettings();

				var auth = serviceProvider.GetService<IAuthenticationTokenProvider>();
				if (auth is not null)
				{
					settings.AuthorizationHeaderValueGetter = () => auth.GetAccessToken();
				}

				settingsBuilder?.Invoke(serviceProvider, settings);
				return settings;
			}),
			configure: configure);
	}
}
