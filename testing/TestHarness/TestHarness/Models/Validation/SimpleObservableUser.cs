namespace TestHarness.Models;
public class SimpleObservableUser : ObservableValidator
{
	private string firstName;
	private string firstNameErrors;
	private string lastName;
	private string lastNameErrors;

	public string FirstNameErrors { get => firstNameErrors; set => SetProperty(ref firstNameErrors, value); }
	public string LastNameErrors { get => lastNameErrors; set => SetProperty(ref lastNameErrors, value); }

	[Required]
	[MinLength(3)]
	[MaxLength(100)]
	public string FirstName { get => firstName; set => SetProperty(ref firstName, value, true); }

	[Required]
	[MinLength(3)]
	[MaxLength(100)]
	public string LastName { get => lastName; set => SetProperty(ref lastName, value, true); }
}
