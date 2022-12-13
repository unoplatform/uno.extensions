namespace Uno.Extensions.Validation;

public static class HostBuilderExtensions
{
	private static bool _isRegistered;
	public static IHostBuilder UseValidation(
		this IHostBuilder hostBuilder)
	{
		if (_isRegistered)
		{
			return hostBuilder;
		}
		_isRegistered = true;
		return hostBuilder
		.ConfigureServices((ctx, services) =>
		{
			_ = services
			.AddScoped(typeof(IValidator<>), typeof(SystemValidator<>))
			.AddTransient<IValidator, Validator>();
		});
	}
}
