namespace Uno.Extensions.Validation;

public static class HostBuilderExtensions
{
	public static IServiceCollection WithValidator<TEntity, TValidator>(
		this IServiceCollection services)
		where TValidator: class, FluentValidation.IValidator<TEntity>
		where TEntity : class
	{
		return services
			.AddInstanceTypeInfo<TEntity>()
			.AddScoped(typeof(IValidator<TEntity>), typeof(FluentValidator<TEntity>))
			.AddScoped<FluentValidation.IValidator<TEntity>, TValidator>();
	}
}
