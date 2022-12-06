namespace Uno.Extensions.Validation;
public static class HostBuilderExtensions
{
	public static IHostBuilder UseValidation(
		this IHostBuilder hostBuilder)
	{
		return hostBuilder
		.ConfigureServices((ctx, services) =>
		{
			_ = services
			.AddScoped(typeof(IValidator<>), typeof(SystemValidator<>));
		});
	}
}
