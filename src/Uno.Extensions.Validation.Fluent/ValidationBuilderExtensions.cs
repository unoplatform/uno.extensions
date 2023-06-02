namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IValidationBuilder"/>
/// </summary>
public static class ValidationBuilderExtensions
{
	/// <summary>
	/// Registers a fluent validator for the specified type.
	/// </summary>
	/// <typeparam name="TEntity">The type to be validated</typeparam>
	/// <typeparam name="TValidator">The validator to register</typeparam>
	/// <param name="builder">The validation builder</param>
	/// <returns>The updated validation builder</returns>
	public static IValidationBuilder Validator<TEntity, TValidator>(
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
