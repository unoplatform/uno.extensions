using System.IO;

namespace Uno.Extensions.Validation;

/// <summary>
/// Defines an interface for a data validator.
/// </summary>
/// <typeparam name="T">Instance to validate</typeparam>
public interface IValidator<in T> : IValidator
{
	/// <summary>
	/// Validate the specified instance asynchronously
	/// </summary>
	/// <param name="instance">Instance to validate</param>
	/// <param name="context">Validation context</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>A list with the validation errors</returns>
	ValueTask<IEnumerable<ValidationResult>> ValidateAsync(T instance, ValidationContext? context = null, CancellationToken cancellationToken = new CancellationToken());
}

public interface IValidator
{
	ValueTask<IEnumerable<ValidationResult>> ValidateAsync(object instance, ValidationContext? context = null, CancellationToken cancellationToken = new CancellationToken());

}

internal interface IInstanceType : IValidator
{
	Type InstanceType { get; }
}

internal record IInstanceTypeWrapper<T>(IServiceProvider Services) : IInstanceType
{
	public Type InstanceType => typeof(T);
	public IValidator<T> Validator => Services.GetRequiredService<IValidator<T>>();
	public ValueTask<IEnumerable<ValidationResult>> ValidateAsync(object instance, ValidationContext? context = null, CancellationToken cancellationToken = default) => Validator.ValidateAsync(instance, context, cancellationToken);
}
