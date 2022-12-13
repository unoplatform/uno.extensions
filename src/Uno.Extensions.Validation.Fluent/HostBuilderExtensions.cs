namespace Uno.Extensions.Validation;

public static class HostBuilderExtensions
{
	public static IHostBuilder UseFluentValidation(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configureDelegate = default)
	{
		hostBuilder
			.UseValidation();

		return configureDelegate is not null ? hostBuilder.ConfigureServices(configureDelegate) : hostBuilder;
	}

	public static IServiceCollection RegisterValidator<TEntity, TValidator>(
		this IServiceCollection services)
		where TValidator: class, FluentValidation.IValidator<TEntity>
		where TEntity : class
	{
		return services
			.AddScoped(typeof(IValidator<TEntity>), typeof(FluentValidator<TEntity>))
			.AddScoped<FluentValidation.IValidator<TEntity>, TValidator>();
	}
}
