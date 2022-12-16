namespace Uno.Extensions.Validation;

public static class HostBuilderExtensions
{
	private static bool _isRegistered;
	public static IHostBuilder UseValidation(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configureDelegate = default)
	{
		if (_isRegistered)
		{
			return hostBuilder;
		}
		_isRegistered = true;
		hostBuilder
		.ConfigureServices((ctx, services) =>
		{
			_ = services
			.AddScoped<IValidator, Validator>();
		});

		return configureDelegate is not null ? hostBuilder.ConfigureServices(configureDelegate) : hostBuilder;
	}

	public static IServiceCollection RegisterEntity<TEntity>(
	this IServiceCollection services)
	where TEntity : class
	{
		return services
			.AddScoped(typeof(IValidator<TEntity>), typeof(SystemValidator<TEntity>))
			.AddInstanceTypeInfo<TEntity>();
	}
}
