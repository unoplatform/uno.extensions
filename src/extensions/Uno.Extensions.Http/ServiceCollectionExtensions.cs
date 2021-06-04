using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MallardMessageHandlers;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Uno.Extensions.Http
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
//                        ContentSerializer = new ObjectSerializerToContentSerializerAdapter(serviceProvider.GetRequiredService<IObjectSerializer>()),
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
                .AddSingleton<INetworkAvailabilityChecker>(new NetworkAvailabilityChecker(GetIsNetworkAvailable))
                .AddTransient<NetworkExceptionHandler>();
        }

        public static IServiceCollection AddExceptionHubHandler(this IServiceCollection services)
        {
            return services
                .AddSingleton<IExceptionHub>(new ExceptionHub())
                .AddTransient<ExceptionHubHandler>();
        }

        //public static IServiceCollection AddAuthenticationTokenHandler(this IServiceCollection services)
        //{
        //    return services
        //        .AddSingleton<IAuthenticationTokenProvider<AuthenticationData>>(s => s.GetRequiredService<IAuthenticationService>() as AuthenticationService)
        //        .AddTransient<AuthenticationTokenHandler<AuthenticationData>>();
        //}

#if (IncludeFirebaseAnalytics)
        private static IServiceCollection AddFirebaseHandler(this IServiceCollection services)
        {
//-:cnd:noEmit
#if __ANDROID__
//+:cnd:noEmit
            return services.AddTransient<FirebasePerformanceHandler>();
//-:cnd:noEmit
#else
//+:cnd:noEmit
            return services;
//-:cnd:noEmit
#endif
//+:cnd:noEmit
        }
#endif

        public static Task<bool> GetIsNetworkAvailable(CancellationToken ct)
        {
            //-:cnd:noEmit
#if WINDOWS_UWP || __ANDROID__ || __IOS__
            // TODO #172362: Not implemented in Uno.
            // return NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            return Task.FromResult(Xamarin.Essentials.Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet);
#else
            return Task.FromResult(true);
#endif
            //+:cnd:noEmit
        }

        public static void AddDefaultHeaders(this HttpClient client, IServiceProvider serviceProvider)
        {
            client.DefaultRequestHeaders.Add("Accept-Language", CultureInfo.CurrentCulture.Name);

            // TODO #172779: Looks like our UserAgent is not of a valid format.
            // TODO #183437: Find alternative for UserAgent.
            // client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", serviceProvider.GetRequiredService<IEnvironmentService>().UserAgent);
        }

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
