using System;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Uno.Extensions.Http.Refit
{
    public static class ServiceCollectionExtensions
    {
//        public static IServiceCollection AddEndpoint<TInterface, TMock>(
//               this IServiceCollection services,
//               HostBuilderContext context,
//               string name,
//               Func<IHttpClientBuilder, IHttpClientBuilder> configure = null
//           )
//               where TInterface : class
//               where TMock : class, TInterface
//        {
//            var options = Options.Create(context.Configuration.GetSection(name).Get<EndpointOptions>());

//            if (options.Value.EnableMock)
//            {
//                services.AddSingleton<TInterface, TMock>();
//            }
//            else
//            {
//                var httpClientBuilder = services
//                    .AddRefitHttpClient<TInterface>(settings: serviceProvider => new RefitSettings()
//                    {
//                        ContentSerializer = new ObjectSerializerToContentSerializerAdapter(serviceProvider.GetRequiredService<ISerializer>()),
//                    })
//                    .ConfigurePrimaryHttpMessageHandler(serviceProvider => serviceProvider.GetRequiredService<HttpMessageHandler>())
//                    .ConfigureHttpClient((serviceProvider, client) =>
//                    {
//                        client.BaseAddress = new Uri(options.Value.Url);
//                        AddDefaultHeaders(client, serviceProvider);
//                    })
//                    .AddHttpMessageHandler<ExceptionHubHandler>();

//                configure?.Invoke(httpClientBuilder);

//                httpClientBuilder.AddHttpMessageHandler<NetworkExceptionHandler>();

//#if (IncludeFirebaseAnalytics)
////-:cnd:noEmit
//#if __ANDROID__
////+:cnd:noEmit
//                httpClientBuilder.AddHttpMessageHandler<FirebasePerformanceHandler>();
////-:cnd:noEmit
//#endif
////+:cnd:noEmit
//#endif
//            }

//            return services;
//        }

        /// <summary>
        /// Adds a Refit client to the service collection.
        /// </summary>
        /// <typeparam name="T">Type of the Refit interface</typeparam>
        /// <param name="services">Service collection</param>
        /// <param name="settings">Optional. Settings to configure the instance with</param>
        /// <returns>Updated IHttpClientBuilder</returns>
        public static IHttpClientBuilder AddRefitHttpClient<T>(this IServiceCollection services, Func<IServiceProvider, RefitSettings> settings = null)
            where T : class
        {
            services.AddSingleton(serviceProvider => RequestBuilder.ForType<T>(settings?.Invoke(serviceProvider)));

            return services
                .AddHttpClient(typeof(T).FullName)
                .AddTypedClient((client, serviceProvider) => RestService.For(client, serviceProvider.GetService<IRequestBuilder<T>>()));
        }
    }
}
