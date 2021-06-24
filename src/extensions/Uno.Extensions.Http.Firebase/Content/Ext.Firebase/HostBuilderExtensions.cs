using Microsoft.Extensions.Hosting;

namespace Uno.Extensions.Http.Firebase
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseFirebaseHandler(this IHostBuilder hostBuilder)
        {
            return hostBuilder
#if __ANDROID__
                    ?.ConfigureServices((ctx, s) =>
                    {
                        _ = s.AddFirebaseHandler();
                    })
#endif
                    ;
        }
    }
}
