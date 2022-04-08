using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;

namespace Uno.Extensions.Http.Refit
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddRefitClient<TInterface>(
			   this IServiceCollection services,
			   HostBuilderContext context,
			   string? name = null,
			   Action<IServiceProvider, RefitSettings>? settingsBuilder = null,
			   Func<IHttpClientBuilder, EndpointOptions, IHttpClientBuilder>? configure = null
		   )
			   where TInterface : class
		{
			return services.AddClient<TInterface>(
				context,
				name,
				(s, c) => HttpClientFactoryExtensions.AddRefitClient<TInterface>(s, settingsAction: serviceProvider =>
				{
					var settings = new RefitSettings()
					{
						ContentSerializer = serviceProvider.GetRequiredService<IHttpContentSerializer>(),
					};

					var auth = serviceProvider.GetService<IAuthenticationToken>();
					if (auth is not null)
					{
						settings.AuthorizationHeaderValueGetter = () => auth.GetAccessToken();
					}

					settingsBuilder?.Invoke(serviceProvider, settings);
					return settings;
				}),
				configure);
		}
	}
}
