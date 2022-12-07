namespace Uno.Extensions.Validation;

/// <summary>
/// Class that can be used to validate objects, properties and methods based for a given ObservableValidator class. 
/// </summary>
public class CommunityToolkitValidator<T>: IValidator<T> where T : ObservableValidator
{
	///<inheritdoc/>
	public ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
		T instance,
		ValidationContext? context = null,
		CancellationToken cancellationToken = default)
	{
		ICollection<ValidationResult> results = new List<ValidationResult>();
		foreach (var error in instance.GetErrors())
		{
			results.Add(new ValidationResult(error.ErrorMessage));
		}

		return new ValueTask<IEnumerable<ValidationResult>>(results.ToList());
	}
}
