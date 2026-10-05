namespace Uno.Extensions.Validation.Tests;

internal static class BclValidation
{
	public static IReadOnlyList<ValidationResult> Validate(object instance)
	{
		var results = new List<ValidationResult>();
		System.ComponentModel.DataAnnotations.Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
		return results;
	}
}

/// <summary>
/// Model exercising the ordering rules of the BCL Validator: Required short-circuits the other attributes of its property,
/// class-level attributes and IValidatableObject only run when there is no previous error.
/// </summary>
[ParityClassLevel]
internal sealed class ParityModel : IValidatableObject
{
	[Required]
	[StringLength(3)]
	public string? Required { get; set; }

	[Display(Name = "Short_Name")]
	[StringLength(3, MinimumLength = 1)]
	[RegularExpression("[a-z]+")]
	public string? Short { get; set; }

	public string? Other { get; set; }

	[Compare(nameof(Other))]
	public string? Confirm { get; set; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		yield return new("validatable", [nameof(Other)]);
	}
}

[AttributeUsage(AttributeTargets.Class)]
internal sealed class ParityClassLevelAttribute : ValidationAttribute
{
	public override bool IsValid(object? value) => false;
}

/// <summary>
/// Model with a property whose type has class-level attributes: the TypeDescriptor merges them in the attributes of the property, but the BCL Validator ignores them.
/// </summary>
internal sealed class ParityNestedModel
{
	[Required]
	public ParityNestedValue? Nested { get; set; }
}

[ParityClassLevel]
[Display(Name = "Nested_Type")]
internal sealed class ParityNestedValue
{
}
