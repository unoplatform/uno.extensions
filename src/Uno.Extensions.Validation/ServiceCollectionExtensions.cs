namespace Uno.Extensions.Validation;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddInstanceTypeInfo<TEntity>(
	this IServiceCollection services
	)where TEntity : class
	{
		return services
			.AddSingleton<IValidatorTypedInstance>(sp => new ValidatorTypedInstance<TEntity>(sp));
		
	}
}

internal interface IValidatorTypedInstance : IValidator
{
	Type InstanceType { get; }
}

internal record ValidatorTypedInstance<T>(IServiceProvider Services) : IValidatorTypedInstance
{
	public Type InstanceType => typeof(T);
	public IValidator<T> Validator => Services.GetRequiredService<IValidator<T>>();
	public ValueTask<IEnumerable<ValidationResult>> ValidateAsync(object instance, ValidationContext? context = null, CancellationToken cancellationToken = default) => Validator.ValidateAsync(instance, context, cancellationToken);
}
