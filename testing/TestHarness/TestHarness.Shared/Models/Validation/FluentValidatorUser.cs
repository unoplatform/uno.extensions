using System.ComponentModel;
using FluentValidation;

namespace TestHarness.Models;

public class ValidationUser : INotifyPropertyChanged
{
	private string _name;
	private string _nameErrors;

	public string Name
	{
		get { return _name; }
		set
		{
			_name = value;
			OnPropertyChanged(nameof(Name));
		}
	}

	public string NameErrors
	{
		get { return _nameErrors; }
		set
		{
			_nameErrors = value;
			OnPropertyChanged(nameof(NameErrors));
		}
	}

	#region PropertyChanged
	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged(string name)
	{
		PropertyChangedEventHandler handler = PropertyChanged;

		if (handler != null)
		{
			handler(this, new PropertyChangedEventArgs(name));
		}
	}
	#endregion
}
public class ValidationUserValidator : AbstractValidator<ValidationUser>
{
	public ValidationUserValidator()
	{
		RuleFor(x => x.Name).Custom((value, context) =>
		{
			var error = "";

			if (value?.Length < 3)
			{
				error = "'Name' field minimum length is 3 characters.";
				context.AddFailure(error);
			}

			if (value?.Length > 6)
			{
				error = "'Name' field maximum length is 6 characters.";
				context.AddFailure(error);
			}

			if (string.IsNullOrEmpty(value))
			{
				error = "'Name' field is required.";
				context.AddFailure(error);
			}

			context.InstanceToValidate.NameErrors = error;
		});
	}
}

