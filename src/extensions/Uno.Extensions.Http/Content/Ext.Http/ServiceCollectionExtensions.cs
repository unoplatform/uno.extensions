using System;
using System.Globalization;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Uno.Extensions.Http.Handlers;

namespace Uno.Extensions.Http
{
    public static class ServiceCollectionExtensions
    {
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
             string name = null,
             Func<IHttpClientBuilder, EndpointOptions, IHttpClientBuilder> configure = null
         )
            where TClient : class
            where TImplementation : class, TClient
        {
            Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder> httpClientFactory = (s, c) => s.AddHttpClient<TClient, TImplementation>();

            return services.AddClient<TClient>(context, name, httpClientFactory, configure);
        }

        public static IServiceCollection AddClient<TInterface>(
              this IServiceCollection services,
              HostBuilderContext context,
              string name = null,
              Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder> httpClientFactory = null,
              Func<IHttpClientBuilder, EndpointOptions, IHttpClientBuilder> configure = null
          )
              where TInterface : class
        {
            name ??= typeof(TInterface).Name;

            if (httpClientFactory is null)
            {
                httpClientFactory = (s, c) => s.AddHttpClient(name);
            }

            var options = context?.Configuration?.GetSection(name)?.Get<EndpointOptions>();

            var httpClientBuilder = httpClientFactory(services, context);

            _ = httpClientBuilder
                .Conditional(
                    options.UseNativeHandler,
                    builder => builder.ConfigurePrimaryHttpMessageHandler<HttpMessageHandler>())
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    client.BaseAddress = new Uri(options.Url);
                    client.Conditional(
                        options.UseDefaultHeaders,
                        c => c.AddDefaultHeaders(serviceProvider));
                })
                .Conditional(
                    options.UseExceptionHubHandler,
                    builder => builder.AddHttpMessageHandler<ExceptionHubHandler>())
                .Conditional(
                    configure is not null,
                    builder => configure(builder, options))
                .Conditional(
                    options.UseNetworkExceptionHandler,
                    builder => builder.AddHttpMessageHandler<NetworkExceptionHandler>());
            return services;
        }

        public static IHttpClientBuilder AddTypedHttpClient<TClient>(
            this IServiceCollection services,
            Func<HttpClient, IServiceProvider, TClient> factory)
           where TClient : class
        {
            return services
                .AddHttpClient(typeof(TClient).FullName)
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

        public static IServiceCollection AddNetworkExceptionHandler(this IServiceCollection services)
        {
            return services
                .AddSingleton<INetworkAvailabilityChecker, NetworkAvailabilityChecker>()
                .AddTransient<NetworkExceptionHandler>();
        }

        public static IServiceCollection AddExceptionHubHandler(this IServiceCollection services)
        {
            return services
                .AddSingleton<IExceptionHub>(new ExceptionHub())
                .AddTransient<ExceptionHubHandler>();
        }

        public static IServiceCollection AddAuthenticationTokenHandler(this IServiceCollection services)
        {
            return services
                .AddSingleton<IAuthenticationTokenProvider<AuthenticationData>>(s => s.GetRequiredService<IAuthenticationService>() as AuthenticationService)
                .AddTransient<AuthenticationTokenHandler<AuthenticationData>>();
        }

#pragma warning disable CA1801, IDE0060, IDE0079 // Review unused parameters - keeping serviceProvider parameter so that the useragent issue can be fixed
        public static HttpClient AddDefaultHeaders(this HttpClient client, IServiceProvider serviceProvider)
#pragma warning restore CA1801, IDE0060, IDE0079 // Review unused parameters
        {
            client?.DefaultRequestHeaders?.Add("Accept-Language", CultureInfo.CurrentCulture.Name);

            // TODO #172779: Looks like our UserAgent is not of a valid format.
            // TODO #183437: Find alternative for UserAgent.
            // client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", serviceProvider.GetRequiredService<IEnvironmentService>().UserAgent);
            return client;
        }
    }
}
