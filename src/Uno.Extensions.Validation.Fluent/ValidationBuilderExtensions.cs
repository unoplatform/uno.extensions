namespace Uno.Extensions.Validation;

public static class ValidationBuilderExtensions
{
	public static IHostBuilder Validator<TEntity, TValidator>(
		this IValidationBuilder builder)
		where TValidator : class, FluentValidation.IValidator<TEntity>
		where TEntity : class
	{
		return builder.ConfigureServices((ctx, services) =>
					services
						.AddInstanceTypeInfo<TEntity>()
						.AddScoped(typeof(IValidator<TEntity>), typeof(FluentValidator<TEntity>))
						.AddScoped<FluentValidation.IValidator<TEntity>, TValidator>())
			.AsValidationBuilder();
	}
}
