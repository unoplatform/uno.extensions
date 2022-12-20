namespace Uno.Extensions.Validation;

public static class HostBuilderExtensions
{
	private static bool _isRegistered;
	public static IHostBuilder UseValidation(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configureDelegate = default,
		Func<IValidationBuilder, IHostBuilder>? configure = default)
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

		hostBuilder= configureDelegate is not null ? hostBuilder.ConfigureServices(configureDelegate) : hostBuilder;
		hostBuilder = configure?.Invoke(hostBuilder.AsValidationBuilder()) ?? hostBuilder;
		return hostBuilder;
	}

	internal static IValidationBuilder AsValidationBuilder(this IHostBuilder hostBuilder)
	{
		if (hostBuilder is IValidationBuilder validationBuilder)
		{
			return validationBuilder;
		}

		return new ValidationBuilder(hostBuilder);
	}
}
