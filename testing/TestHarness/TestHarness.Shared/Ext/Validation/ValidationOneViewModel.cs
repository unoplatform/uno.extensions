
using static FluentValidation.DefaultValidatorExtensions;

namespace TestHarness.Ext.Navigation.Validation;

[ReactiveBindable(false)]
public partial class ValidationOneViewModel : ObservableObject
{
	private readonly IValidator<SimpleEntity> _simpleValidator;
	private readonly IValidator<ValidationUser> _userValidator;
	public ValidationOneViewModel(
		IValidator<SimpleEntity> simpleValidator,
		IValidator<ValidationUser> userValidator)
	{
		_simpleValidator = simpleValidator;
		_userValidator = userValidator;

		_ = ValidateEntities();
	}

	private async Task ValidateEntities()
	{
		var entity = new SimpleEntity();
		var results = await _simpleValidator.ValidateAsync(entity);

		var user = new ValidationUser();
		var userResult = await _userValidator.ValidateAsync(user);
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

public record ValidationUser(string? Name = default);

public class ValidationUserValidator : FluentValidation.AbstractValidator<ValidationUser>
{
	public ValidationUserValidator()
	{
		RuleFor(x => x.Name).NotNull();
	}
}

