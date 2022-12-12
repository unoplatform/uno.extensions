
using static FluentValidation.DefaultValidatorExtensions;

namespace TestHarness.Ext.Navigation.Validation;

[ReactiveBindable(false)]
public partial class ValidationOneViewModel : ObservableObject
{
	private readonly IValidator<SimpleEntity> _simpleValidator;
	private readonly IValidator<ValidationUser> _userValidator;
	private readonly IValidator<SimpleObservableUser> _simpleObservableUser;
	public ValidationOneViewModel(
		IValidator<SimpleEntity> simpleValidator,
		IValidator<ValidationUser> userValidator,
		IValidator<SimpleObservableUser> simpleObservableUser)
	{
		_simpleValidator = simpleValidator;
		_userValidator = userValidator;
		_simpleObservableUser = simpleObservableUser;

		_ = ValidateEntities();
	}

	private async Task ValidateEntities()
	{
		var entity = new SimpleEntity();
		var results = await _simpleValidator.ValidateAsync(entity);

		var user = new ValidationUser();
		var userResult = await _userValidator.ValidateAsync(user);

		var observableUser = new SimpleObservableUser();
		var observableUserResult = _simpleObservableUser.ValidateAsync(observableUser);
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

