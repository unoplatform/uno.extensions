using System.ComponentModel.DataAnnotations;

namespace Playground.Models;

/// <summary>
/// A person edited on the <see cref="Views.ValidationPage"/>, which validates itself (cf. <see cref="IValidatableObject"/>).
/// </summary>
/// <remarks>
/// This record is used as the value of an MVUX state, so a bindable (BindablePersonViewModel) is generated for it
/// and the validation results targeting its members are exposed by that bindable through INotifyDataErrorInfo.
/// </remarks>
public record PersonModel(string FirstName, string LastName) : IValidatableObject
{
	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (string.IsNullOrWhiteSpace(FirstName))
		{
			yield return new ValidationResult("First name is required.", new[] { nameof(FirstName) });
		}

		if (string.IsNullOrWhiteSpace(LastName))
		{
			yield return new ValidationResult("Last name is required.", new[] { nameof(LastName) });
		}
		else if (LastName.Length < 2)
		{
			yield return new ValidationResult("Last name must have at least 2 characters.", new[] { nameof(LastName) });
		}

		// Entity-level error (no member names): exposed by GetErrors(null) on the person bindable,
		// and by GetErrors("Person") on the page view model.
		if (!string.IsNullOrWhiteSpace(FirstName)
			&& string.Equals(FirstName.Trim(), LastName?.Trim(), StringComparison.OrdinalIgnoreCase))
		{
			yield return new ValidationResult("First and last names must be different.");
		}
	}
}
