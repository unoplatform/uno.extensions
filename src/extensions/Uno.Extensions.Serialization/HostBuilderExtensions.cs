using Microsoft.Extensions.Hosting;

namespace Uno.Extensions.Serialization;

public static class HostBuilderExtensions
{
    public static IHostBuilder? UseSerialization(this IHostBuilder hostBuilder)
    {
        if (hostBuilder is not null)
        {
            return hostBuilder
                    .ConfigureServices((ctx, s) =>
                    {
                        _ = s.AddSystemTextJsonSerialization();
                    });
        }

        return default;
    }
}
