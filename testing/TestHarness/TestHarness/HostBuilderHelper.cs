namespace TestHarness;

internal static class HostBuilderHelper
{
	public static IHostBuilder Use(this IHostBuilder builder, Func<IHostBuilder, IHostBuilder> func)
	{
		return func(builder);
	}
}
