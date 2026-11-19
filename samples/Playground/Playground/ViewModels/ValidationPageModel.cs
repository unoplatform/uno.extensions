using System.ComponentModel.DataAnnotations;
using Uno.Extensions.Reactive;
using Uno.Extensions.Validation;

namespace Playground.ViewModels;

/// <summary>
/// MVUX model of the <see cref="Views.ValidationPage"/> which demonstrates the validation of states (cf. State.Validate)
/// and of the parameter of a command when it is executed (cf. the Validation of the command builder).
/// </summary>
public partial record ValidationPageModel(IValidator Validator)
{
	private const int ReasonMinLength = 10;

	/// <summary>
	/// A record state, validated using the <see cref="IValidator"/> service (which runs the <see cref="PersonModel.Validate"/>).
	/// Errors targeting FirstName / LastName are routed to the generated person bindable.
	/// </summary>
	public IState<PersonModel> Person => State.Value(this, () => new PersonModel(string.Empty, string.Empty))
		.Validate(Validator);

	/// <summary>
	/// A primitive state, validated inline. Errors are exposed by the page view model itself (GetErrors("Reason")).
	/// </summary>
	public IState<string> Reason => State.Value(this, () => string.Empty)
		.Validate(ValidateReason);

	/// <summary>
	/// A record state which is not validated on change: it is validated only when the <see cref="Submit"/> command is executed,
	/// and its errors remain until the next submit (even if the values are fixed meanwhile).
	/// </summary>
	public IState<PersonModel> Contact => State.Value(this, () => new PersonModel(string.Empty, string.Empty));

	/// <summary>
	/// The result of the last successful submit.
	/// </summary>
	public IState<string> Submitted => State<string>.Empty(this);

	/// <summary>
	/// Validates the <see cref="Contact"/> (using the <see cref="IValidator"/> service) each time it is executed,
	/// and only invokes the action if it is valid.
	/// </summary>
	public IAsyncCommand Submit => Command.Create(b => b
		.Given(Contact)
		.Validation(Validator)
		.Then(async (contact, ct) => await Submitted.SetAsync($"Submitted {contact.FirstName} {contact.LastName} at {DateTimeOffset.Now:T}", ct)));

	public async ValueTask Fill(CancellationToken ct)
	{
		await Person.UpdateAsync(_ => new PersonModel("John", "Doe"), ct);
		await Reason.SetAsync("Testing the MVUX validation", ct);
	}

	public async ValueTask Reset(CancellationToken ct)
	{
		await Person.UpdateAsync(_ => new PersonModel(string.Empty, string.Empty), ct);
		await Reason.SetAsync(string.Empty, ct);
	}

	private static async ValueTask<IEnumerable<ValidationResult>> ValidateReason(string reason, CancellationToken ct)
	{
		// Simulate an async validation (e.g. a server-side check), so we can observe that typing quickly cancels the pending validations.
		await Task.Delay(300, ct);

		if (string.IsNullOrWhiteSpace(reason))
		{
			return new[] { new ValidationResult("A reason is required.", new[] { nameof(Reason) }) };
		}

		if (reason.Trim().Length < ReasonMinLength)
		{
			return new[] { new ValidationResult($"The reason must have at least {ReasonMinLength} characters.", new[] { nameof(Reason) }) };
		}

		return Enumerable.Empty<ValidationResult>();
	}
}
