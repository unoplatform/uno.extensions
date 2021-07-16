using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Refit;
using Uno.Extensions.Http.Handlers;
using Uno.Extensions.Serialization;

namespace Uno.Extensions.Http.Refit
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRefitClient<TInterface>(
               this IServiceCollection services,
               HostBuilderContext context,
               string name,
               Func<IHttpClientBuilder, EndpointOptions, IHttpClientBuilder> configure = null
           )
               where TInterface : class
        {
            return services.AddClient<TInterface>(
                context,
                name,
                (s, c) => s.AddRefitHttpClient<TInterface>(settings: serviceProvider => new RefitSettings()
                {
                    ContentSerializer = serviceProvider.GetRequiredService<IHttpContentSerializer>(),
                }),
                configure);
        }

        /// <summary>
        /// Adds a Refit client to the service collection.
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <param name="settings">Optional. Settings to configure the instance with.</param>
        /// <returns>Updated IHttpClientBuilder.</returns>
        public static IHttpClientBuilder AddRefitHttpClient<T>(this IServiceCollection services, Func<IServiceProvider, RefitSettings> settings = null)
            where T : class
        {
            services.AddSingleton(serviceProvider => RequestBuilder.ForType<T>(settings?.Invoke(serviceProvider)));

            return services
                .AddTypedHttpClient((client, serviceProvider) => RestService.For(client, serviceProvider.GetService<IRequestBuilder<T>>()));
        }
    }
}
