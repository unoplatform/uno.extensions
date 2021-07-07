using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Http.Firebase
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFirebaseHandler(this IServiceCollection services)
        {
            return services
#if __ANDROID__
                .AddTransient<FirebasePerformanceHandler>()
#endif
                ;
        }
    }

    public static class HttpClientBuilderExtensions
    {

        public static IHttpClientBuilder AddFirebaseHandler(this IHttpClientBuilder builder)
        {
            return builder
#if __ANDROID__
                .AddHttpMessageHandler<FirebasePerformanceHandler>()
#endif
                ;
        }
    }

    public static class EndpointOptionsExtensions
    {
        public static bool UseFirebaseHandler(this EndpointOptions options)
        {
            return options.FeatureEnabled(nameof(UseFirebaseHandler));
        }
    }
}
