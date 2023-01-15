
using System.Collections.ObjectModel;
using static FluentValidation.DefaultValidatorExtensions;

namespace TestHarness.Ext.Navigation.Validation;

[ReactiveBindable(false)]
public partial class ValidationOneViewModel : ObservableObject
{
	private readonly IValidator _validator;

	[ObservableProperty]
	private List<ValidationResult> validatableObjectErrors;

	[ObservableProperty]
	private List<ValidationResult> observableValidatorErrors;

	[ObservableProperty]
	private List<ValidationResult> fluentValidatorErrors;
	public ValidationOneViewModel(IValidator validator)
	{
		_validator = validator;

		_ = ValidateEntities();
	}

	private async Task ValidateEntities()
	{
		var entity = new SimpleEntity();
		var errors = await _validator.ValidateAsync(entity);
		ValidatableObjectErrors = new List<ValidationResult>(errors);

		var user = new ValidationUser();
		errors = await _validator.ValidateAsync(user);
		FluentValidatorErrors = new List<ValidationResult>(errors);

		var observableUser = new SimpleObservableUser();
		errors = await _validator.ValidateAsync(observableUser);
		ObservableValidatorErrors = new List<ValidationResult>(errors);
	}


	public class SimpleEntity : IValidatableObject
	{
		public string Title { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			return string.IsNullOrWhiteSpace(Title) ?
				new ValidationResult[] {
					new ValidationResult(
						errorMessage: "Title should not be null",
						memberNames: new[] {nameof(Title) }
						)
				} :
				default;
		}
	}


}

public class SimpleObservableUser : ObservableValidator
{
	private string firstName;
	private string lastName;

	[Required]
	[MinLength(2)]
	[MaxLength(100)]
	public string First { get => firstName; set => SetProperty(ref firstName, value, true); }

	[Required]
	[MinLength(2)]
	[MaxLength(100)]
	public string Last { get => lastName; set => SetProperty(ref lastName, value, true); }
}

public record ValidationUser(string? Name = default);

public class ValidationUserValidator : FluentValidation.AbstractValidator<ValidationUser>
{
	public ValidationUserValidator()
	{
		RuleFor(x => x.Name).NotNull();
	}
}

