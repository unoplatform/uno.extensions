using System.ComponentModel;

namespace Uno.Extensions.Validation;

/// <summary>
/// Class that can be used to validate objects, properties and methods based on the associated ValidationAttributes. 
/// </summary>
/// <typeparam name="T">Type</typeparam>
public class SystemValidator<T> : IValidator<T> where T: class
{
	///<inheritdoc/>
	public ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
		T instance,
		ValidationContext? context = null,
		CancellationToken cancellationToken = default)
	{
        ICollection<ValidationResult> results = new List<ValidationResult>();

		context ??= new ValidationContext(instance);

		var succeeded = Validator.TryValidateObject(instance, context, results, true);

		if (!succeeded && instance is INotifyDataErrorInfo _instance)
		{
			results = _instance?.GetErrors(null).OfType<ValidationResult>()?.ToList()
				?? new List<ValidationResult>();
		}

		return new ValueTask<IEnumerable<ValidationResult>>(results.ToList());
	}
}
