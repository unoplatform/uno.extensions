namespace Uno.Extensions.Validation.Fluent;

/// <summary>
/// Class that can be used to validate objects, properties and methods based on the associated Fluent Validators. 
/// </summary>
/// <typeparam name="T">Type</typeparam>
internal class FluentValidator<T> : IValidator<T>
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
		var result = new List<ValidationResult>();

		if (instance is T tInstance)
		{
			var validationResult = (await _validator.ValidateAsync(tInstance, cancellationToken));
			result = validationResult?.Errors.Select(x => new ValidationResult(x.ErrorMessage, new[] { x.PropertyName }))?.ToList();
		}

		return result ?? new List<ValidationResult>();
	}
}

