namespace Uno.Extensions.Validation;

internal static class ServiceCollectionExtensions
{
	internal static IServiceCollection AddInstanceTypeInfo<TEntity>(
		this IServiceCollection services)
		where TEntity : class => services
			.AddSingleton<IValidatorTypedInstance>(sp => new ValidatorTypedInstance<TEntity>(sp));
}
