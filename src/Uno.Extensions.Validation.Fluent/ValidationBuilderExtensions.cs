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
						.AddSingleton(typeof(IValidator<TEntity>), typeof(FluentValidator<TEntity>))
						.AddSingleton<FluentValidation.IValidator<TEntity>, TValidator>())
			.AsValidationBuilder();
	}
}
