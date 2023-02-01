namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseSerialization(this IHostBuilder hostBuilder, Action<IServiceCollection> configure)
	{
		return hostBuilder.UseSerialization((context, builder) => configure.Invoke(builder));
	}

	public static IHostBuilder UseSerialization(this IHostBuilder hostBuilder, Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		return hostBuilder
				.ConfigureServices((ctx, s) =>
				{
					_ = s.AddSystemTextJsonSerialization(ctx);
					configure?.Invoke(ctx, s);
				});
	}
}
