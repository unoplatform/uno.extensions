namespace Uno.Extensions.Serialization;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseSerialization(this IHostBuilder hostBuilder, Action<IServiceCollection>? jsonTypeCallback = default)
    {
        return hostBuilder
                .ConfigureServices((ctx, s) =>
				{
					_=s.AddSingleton(typeof(IJsonDataService<>), typeof(JsonDataService<>))
						.AddSystemTextJsonSerialization();
					jsonTypeCallback?.Invoke(s);
				});
    }
}
