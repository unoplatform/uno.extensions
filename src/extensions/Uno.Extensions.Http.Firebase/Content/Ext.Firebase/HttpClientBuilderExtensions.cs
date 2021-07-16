using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Http.Firebase
{
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
}
