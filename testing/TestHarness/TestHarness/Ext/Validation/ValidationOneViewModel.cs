using TestHarness.Models;
using System.ComponentModel;

namespace TestHarness.Ext.Navigation.Validation;

[ReactiveBindable(false)]
public partial class ValidationOneViewModel : ObservableObject
{
	private readonly IValidator _validator;

	[ObservableProperty]
	private SimpleEntity simpleEntity;

	[ObservableProperty]
	private ValidationUser fluentUser;

	[ObservableProperty]
	private SimpleObservableUser observableUser;

	public ValidationOneViewModel(IValidator validator)
	{
		_validator = validator;
		_ = ValidateEntities();
	}

	private async Task ValidateEntities()
	{
		SimpleEntity = new SimpleEntity();
		SimpleEntity.PropertyChanged += async (s, e) => { if (!e.PropertyName.Contains("Error")) await ValidateSimpleEntity(SimpleEntity, e.PropertyName); };
		await ValidateSimpleEntity(SimpleEntity);

		FluentUser = new ValidationUser();
		//Ignore binded Error properties and trigger validation when any other property is updated
		//Individual binded error properties are updated in AbstractValidator definition using a custom rule definition
		FluentUser.PropertyChanged += async (s, e) => { if (!e.PropertyName.Contains("Error")) await _validator.ValidateAsync(FluentUser); };
		await _validator.ValidateAsync(FluentUser);

		ObservableUser = new SimpleObservableUser();
		ObservableUser.PropertyChanged += async (s, e) => { if (e.PropertyName != nameof(ObservableUser.HasErrors)) await ValidateObservableUser(ObservableUser, e.PropertyName); };
		await ValidateObservableUser(ObservableUser);
	}

	private async Task ValidateSimpleEntity(SimpleEntity entity, string propertyName = null)
	{
		if (entity == null) return;
		var errors = await _validator.ValidateAsync(entity);

		//Once we have all the errors we need to set binded error properties individually
		if (!string.IsNullOrEmpty(propertyName) && errors.Any())
		{
			//Display only the errors needed
			if (propertyName == nameof(entity.Title)) entity.TitleErrors = string.Join(Environment.NewLine, from ValidationResult e in errors where e.MemberNames.Contains(propertyName) select e.ErrorMessage);
		}
		else if (!string.IsNullOrEmpty(propertyName) && !errors.Any())
		{
			//Clean last messages if not errors found
			if (propertyName == nameof(entity.Title)) entity.TitleErrors = "";
		}
		else if (string.IsNullOrEmpty(propertyName) && errors.Any())
		{
			//All errors
			entity.TitleErrors = string.Join(Environment.NewLine, from ValidationResult e in errors where e.MemberNames.Contains(nameof(entity.Title)) select e.ErrorMessage);}
	}

	private async Task ValidateObservableUser(SimpleObservableUser user, string propertyName = null)
	{
		if (user == null) return;
		var errors = await _validator.ValidateAsync(user);

		//Once we have all the errors we need to set binded error properties individually
		if (!string.IsNullOrEmpty(propertyName) && errors.Any())
		{
			//Display only the errors needed
			if (propertyName == nameof(user.FirstName)) user.FirstNameErrors = string.Join(Environment.NewLine, from ValidationResult e in errors where e.MemberNames.Contains(propertyName) select e.ErrorMessage);
			if (propertyName == nameof(user.LastName)) user.LastNameErrors = string.Join(Environment.NewLine, from ValidationResult e in errors where e.MemberNames.Contains(propertyName) select e.ErrorMessage);
		}
		else if (!string.IsNullOrEmpty(propertyName) && !errors.Any())
		{
			//Clean last messages if not errors found
			if (propertyName == nameof(user.FirstName)) user.FirstNameErrors = "";
			if (propertyName == nameof(user.LastName)) user.LastNameErrors = "";
		}
		else if (string.IsNullOrEmpty(propertyName) && errors.Any())
		{
			//All errors
			user.FirstNameErrors = string.Join(Environment.NewLine, from ValidationResult e in errors where e.MemberNames.Contains(nameof(user.FirstName)) select e.ErrorMessage);
			user.LastNameErrors = string.Join(Environment.NewLine, from ValidationResult e in errors where e.MemberNames.Contains(nameof(user.LastName)) select e.ErrorMessage);
		}
	}
}



