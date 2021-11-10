using System;
using System.Globalization;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            var options = context.Configuration.GetSection(name).Get<EndpointOptions>();

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
                    builder => builder.ConfigurePrimaryHttpMessageHandler<HttpMessageHandler>())
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
                new Xamarin.Android.Net.AndroidClientHandler()
#elif NETFX_CORE
                new WinHttpHandler()
#else
                new HttpClientHandler()
#endif
            );
        }
    }
}
