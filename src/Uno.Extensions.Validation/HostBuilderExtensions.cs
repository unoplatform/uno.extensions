namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IHostBuilder"/> to add validation.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Registers the validation services.
	/// </summary>
	/// <param name="hostBuilder">The host builder to register validation services with</param>
	/// <param name="configureDelegate">Callback for configuring host builder</param>
	/// <param name="configure">Callback for configuring additional validation services (eg Fluent)</param>
	/// <returns></returns>
	public static IHostBuilder UseValidation(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configureDelegate = default,
		Func<IValidationBuilder, IHostBuilder>? configure = default)
	{
		if (hostBuilder.IsRegistered(nameof(UseValidation)))
		{
			return hostBuilder;
		}

		hostBuilder
		.ConfigureServices((ctx, services) =>
		{
			_ = services
			.AddSingleton<IValidator, Validator>();
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
