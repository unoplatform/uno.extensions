using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxValidationModel
{
	public IState<string> Name => State.Value(this, () => "initial")
		.Validate(async (name, ct) => string.IsNullOrWhiteSpace(name)
			? new[] { new ValidationResult("Name is required", new[] { nameof(Name) }) }
			: Enumerable.Empty<ValidationResult>());

	public IState<MvuxValidationPerson> Person => State.Value(this, () => new MvuxValidationPerson("John"))
		.Validate(async (person, ct) => string.IsNullOrWhiteSpace(person.FirstName)
			? new[] { new ValidationResult("First name is required", new[] { nameof(MvuxValidationPerson.FirstName) }) }
			: Enumerable.Empty<ValidationResult>());
}

public partial record MvuxValidationPerson(string FirstName);
