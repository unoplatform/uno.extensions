namespace Uno.Extensions.Validation;

internal record ValidatorTypedInstance<T>(IServiceProvider Services) : IValidatorTypedInstance
{
	public Type InstanceType => typeof(T);
	public IValidator<T> Validator => Services.GetRequiredService<IValidator<T>>();
	public ValueTask<IEnumerable<ValidationResult>> ValidateAsync(object instance, ValidationContext? context = null, CancellationToken cancellationToken = default) => Validator.ValidateAsync(instance, context, cancellationToken);
}

