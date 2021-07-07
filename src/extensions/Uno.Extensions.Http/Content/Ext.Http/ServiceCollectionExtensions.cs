using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Http.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Uno.Extensions.Http
{
    public static class ServiceCollectionExtensions
    {
        public static T Conditional<T>(
            this T builder,
            bool predicate,
            Func<T, T> configureBuilder)
        {
            return predicate ? configureBuilder(builder) : builder;
        }

        public static IServiceCollection AddClient<TInterface>(
              this IServiceCollection services,
              HostBuilderContext context,
              string name,
              Func<IServiceCollection, HostBuilderContext, IHttpClientBuilder> httpClientFactory,
              Func<IHttpClientBuilder, IHttpClientBuilder> configure = null
          )
              where TInterface : class
        {
            var options = context.Configuration.GetSection(name).Get<EndpointOptions>();

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
                    builder => configure(builder))
                .Conditional(
                    options.UseNetworkExceptionHandler,
                    builder => builder.AddHttpMessageHandler<NetworkExceptionHandler>());
            return services;
        }

        public static IServiceCollection AddClient<TInterface>(
             this IServiceCollection services,
             HostBuilderContext context,
             string name,
             Func<IHttpClientBuilder, IHttpClientBuilder> configure = null
         )
             where TInterface : class
        {
            return services.AddClient<TInterface>(context, name, (s, c) => s.AddHttpClient(name));
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

        public static HttpClient AddDefaultHeaders(this HttpClient client, IServiceProvider serviceProvider)
        {
            client.DefaultRequestHeaders.Add("Accept-Language", CultureInfo.CurrentCulture.Name);

            // TODO #172779: Looks like our UserAgent is not of a valid format.
            // TODO #183437: Find alternative for UserAgent.
            // client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", serviceProvider.GetRequiredService<IEnvironmentService>().UserAgent);
            return client;
        }
    }
}
