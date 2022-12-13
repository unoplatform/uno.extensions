namespace Uno.Extensions.Validation;

/// <summary>
/// Class that can be used to validate objects, properties and methods based on the associated Fluent Validators. 
/// </summary>
/// <typeparam name="T">Type</typeparam>
public class FluentValidator<T> : IValidator
{
	/// <summary>
	/// Fluent validator
	/// </summary>
	private readonly FluentValidation.IValidator<T> _validator;

	public FluentValidator(FluentValidation.IValidator<T> validator)
	{
		_validator = validator;
	}

	///<inheritdoc/>
	public async ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
		object instance,
		ValidationContext? context = null,
		CancellationToken cancellationToken = default)
	{
		List<ValidationResult> result = new List<ValidationResult>();
		
		var validationResult = (await _validator.ValidateAsync((T)instance, cancellationToken));
		validationResult?.Errors.ForEach(error =>
		{
			result.Add(new ValidationResult(error.ErrorMessage));
		});

		return result;
	}
}

