using System.ComponentModel;

namespace Uno.Extensions.Validation;

/// <summary>
/// Class that can be used to validate objects, properties and methods for a given ObservableValidator class. 
/// </summary>
public class CommunityToolkitValidator<T>: IValidator<T> where T : ObservableValidator
{
	///<inheritdoc/>
	public ValueTask<IEnumerable<ValidationResult>> ValidateAsync(
		object instance,
		ValidationContext? context = null,
		CancellationToken cancellationToken = default)
	{
		ICollection<ValidationResult> results = new List<ValidationResult>();
		if (instance is ObservableValidator _instance)
		{
			results = _instance.GetErrors().ToList();
		}

		return new ValueTask<IEnumerable<ValidationResult>>(results.ToList());
	}
}
