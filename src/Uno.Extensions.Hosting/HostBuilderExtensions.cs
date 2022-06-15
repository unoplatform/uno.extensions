namespace Uno.Extensions.Hosting;

public static class HostBuilderExtensions
{
	public static TBuilder AsBuilder<TBuilder>(this IHostBuilder hostBuilder) where TBuilder:IBuilder, new()
	{
		if (hostBuilder is TBuilder builder)
		{
			return builder;
		}

		return new TBuilder { HostBuilder = hostBuilder };
	}
}
