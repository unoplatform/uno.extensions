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
	/// <param name="name">[optional]The name for locating endpoint information in appsettings</param>
	/// <param name="settingsBuilder">[optional]Callback for overriding Refit settings</param>
	/// <param name="configure">[optional]Callback for configuring the endpoint</param>
	/// <returns></returns>
	public static IServiceCollection AddRefitClient<TInterface>(
		   this IServiceCollection services,
		   HostBuilderContext context,
		   string? name = null,
		   Action<IServiceProvider, RefitSettings>? settingsBuilder = null,
		   Func<IHttpClientBuilder, EndpointOptions?, IHttpClientBuilder>? configure = null
	   )
		   where TInterface : class
	{
		return services.AddClient<TInterface>(
			context,
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
