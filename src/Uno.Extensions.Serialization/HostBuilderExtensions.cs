namespace Uno.Extensions.Serialization;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseSerialization(this IHostBuilder hostBuilder, Action<IServiceCollection>? jsonTypeCallback = default)
    {
        return hostBuilder
                .ConfigureServices((ctx, s) =>
				{
					_=s.AddSystemTextJsonSerialization();
					jsonTypeCallback?.Invoke(s);
				});
    }
}
