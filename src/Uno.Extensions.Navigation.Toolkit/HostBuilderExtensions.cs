namespace Uno.Extensions;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseToolkitNavigation(
			this IHostBuilder builder)
	{
		return builder
			.UseToolkit()
			.ConfigureServices((ctx,sp) =>
			{
				_ = sp.AddToolkitNavigation(ctx);
			});
	}
}
