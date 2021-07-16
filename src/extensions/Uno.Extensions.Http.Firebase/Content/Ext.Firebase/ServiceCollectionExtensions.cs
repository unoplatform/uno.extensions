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
}
