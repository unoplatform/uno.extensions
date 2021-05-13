using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate;
using ApplicationTemplate.Business;
using ApplicationTemplate.Client;
using GeneratedSerializers;
using MallardMessageHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Refit;

namespace ApplicationTemplate
{
    /// <summary>
    /// This class is used for API configuration.
    /// - Configures API endpoints.
    /// - Configures HTTP handlers.
    /// </summary>
    public static class ApiConfiguration
    {
        /// <summary>
        /// Adds the API services to the <see cref="IHostBuilder"/>.
        /// </summary>
        /// <param name="hostBuilder">Host builder.</param>
        /// <returns><see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder AddApi(this IHostBuilder hostBuilder)
        {
            if (hostBuilder is null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            return hostBuilder.ConfigureServices((context, s) => s
                .AddMainHandler()
                .AddNetworkExceptionHandler()
                .AddExceptionHubHandler()
                .AddAuthenticationTokenHandler()
#if (IncludeFirebaseAnalytics)
                .AddFirebaseHandler()
#endif
                .AddResponseContentDeserializer()
                .AddAuthenticationEndpoint(context)
                .AddPostEndpoint(context)
                .AddUserProfileEndpoint(context)
                .AddChuckNorrisEndpoint(context)
            );
        }

        private static IServiceCollection AddUserProfileEndpoint(this IServiceCollection services, HostBuilderContext context)
        {
            return services.AddEndpoint<IUserProfileEndpoint, UserProfileEndpointMock>(context, "UserProfileEndpoint");
        }

        private static IServiceCollection AddAuthenticationEndpoint(this IServiceCollection services, HostBuilderContext context)
        {
            return services.AddEndpoint<IAuthenticationEndpoint, AuthenticationEndpointMock>(context, "AuthenticationEndpoint");
        }

        private static IServiceCollection AddPostEndpoint(this IServiceCollection services, HostBuilderContext context)
        {
            return services
                .AddSingleton<IErrorResponseInterpreter<PostErrorResponse>>(s => new ErrorResponseInterpreter<PostErrorResponse>(
                    (request, response, deserializedResponse) => deserializedResponse.Error != null,
                    (request, response, deserializedResponse) => new PostEndpointException(deserializedResponse)
                ))
                .AddTransient<ExceptionInterpreterHandler<PostErrorResponse>>()
                .AddEndpoint<IPostEndpoint, PostEndpointMock>(context, "PostEndpoint", b => b
                    .AddHttpMessageHandler<ExceptionInterpreterHandler<PostErrorResponse>>()
                    .AddHttpMessageHandler<AuthenticationTokenHandler<AuthenticationData>>()
                );
        }

        private static IServiceCollection AddChuckNorrisEndpoint(this IServiceCollection services, HostBuilderContext context)
        {
            return services
                .AddSingleton<IErrorResponseInterpreter<ChuckNorrisErrorResponse>>(s => new ErrorResponseInterpreter<ChuckNorrisErrorResponse>(
                    (request, response, deserializedResponse) => deserializedResponse.Message != null,
                    (request, response, deserializedResponse) => new ChuckNorrisException(deserializedResponse.Message)
                ))
                .AddTransient<ExceptionInterpreterHandler<ChuckNorrisErrorResponse>>()
                .AddEndpoint<IChuckNorrisEndpoint, ChuckNorrisEndpointMock>(context, "ChuckNorrisEndpoint", b => b
                    .AddHttpMessageHandler<ExceptionInterpreterHandler<ChuckNorrisErrorResponse>>()
                );
        }

        private static IServiceCollection AddEndpoint<TInterface, TMock>(
            this IServiceCollection services,
            HostBuilderContext context,
            string name,
            Func<IHttpClientBuilder, IHttpClientBuilder> configure = null
        )
            where TInterface : class
            where TMock : class, TInterface
        {
            var options = Options.Create(context.Configuration.GetSection(name).Get<EndpointOptions>());

            if (options.Value.EnableMock)
            {
                services.AddSingleton<TInterface, TMock>();
            }
            else
            {
                var httpClientBuilder = services
                    .AddRefitHttpClient<TInterface>(settings: serviceProvider => new RefitSettings()
                    {
                        ContentSerializer = new ObjectSerializerToContentSerializerAdapter(serviceProvider.GetRequiredService<IObjectSerializer>()),
                    })
                    .ConfigurePrimaryHttpMessageHandler(serviceProvider => serviceProvider.GetRequiredService<HttpMessageHandler>())
                    .ConfigureHttpClient((serviceProvider, client) =>
                    {
                        client.BaseAddress = new Uri(options.Value.Url);
                        AddDefaultHeaders(client, serviceProvider);
                    })
                    .AddHttpMessageHandler<ExceptionHubHandler>();

                configure?.Invoke(httpClientBuilder);

                httpClientBuilder.AddHttpMessageHandler<NetworkExceptionHandler>();

#if (IncludeFirebaseAnalytics)
//-:cnd:noEmit
#if __ANDROID__
//+:cnd:noEmit
                httpClientBuilder.AddHttpMessageHandler<FirebasePerformanceHandler>();
//-:cnd:noEmit
#endif
//+:cnd:noEmit
#endif
            }

            return services;
        }

        private static IServiceCollection AddMainHandler(this IServiceCollection services)
        {
            return services.AddTransient<HttpMessageHandler>(s =>
//-:cnd:noEmit
#if __IOS__
//+:cnd:noEmit
                new NSUrlSessionHandler()
//-:cnd:noEmit
#elif __ANDROID__
//+:cnd:noEmit
                new Xamarin.Android.Net.AndroidClientHandler()
//-:cnd:noEmit
#else
//+:cnd:noEmit
                new HttpClientHandler()
//-:cnd:noEmit
#endif
//+:cnd:noEmit
            );
        }

        private static IServiceCollection AddResponseContentDeserializer(this IServiceCollection services)
        {
            return services.AddSingleton<IResponseContentDeserializer, ObjectSerializerToResponseContentDeserializer>();
        }

        private static IServiceCollection AddNetworkExceptionHandler(this IServiceCollection services)
        {
            return services
                .AddSingleton<INetworkAvailabilityChecker>(new NetworkAvailabilityChecker(GetIsNetworkAvailable))
                .AddTransient<NetworkExceptionHandler>();
        }

        private static IServiceCollection AddExceptionHubHandler(this IServiceCollection services)
        {
            return services
                .AddSingleton<IExceptionHub>(new ExceptionHub())
                .AddTransient<ExceptionHubHandler>();
        }

        private static IServiceCollection AddAuthenticationTokenHandler(this IServiceCollection services)
        {
            return services
                .AddSingleton<IAuthenticationTokenProvider<AuthenticationData>>(s => s.GetRequiredService<IAuthenticationService>() as AuthenticationService)
                .AddTransient<AuthenticationTokenHandler<AuthenticationData>>();
        }

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

        private static Task<bool> GetIsNetworkAvailable(CancellationToken ct)
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

        private static void AddDefaultHeaders(HttpClient client, IServiceProvider serviceProvider)
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
        private static IHttpClientBuilder AddRefitHttpClient<T>(this IServiceCollection services, Func<IServiceProvider, RefitSettings> settings = null)
            where T : class
        {
            services.AddSingleton(serviceProvider => RequestBuilder.ForType<T>(settings?.Invoke(serviceProvider)));

            return services
                .AddHttpClient(typeof(T).FullName)
                .AddTypedClient((client, serviceProvider) => RestService.For(client, serviceProvider.GetService<IRequestBuilder<T>>()));
        }

        private class EndpointOptions
        {
            public string Url { get; set; }

            public bool EnableMock { get; set; }
        }
    }
}
