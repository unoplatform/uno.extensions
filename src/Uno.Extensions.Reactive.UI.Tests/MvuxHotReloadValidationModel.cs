using System.ComponentModel.DataAnnotations;

namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadValidationModel
{
	public IState<string> Name => State.Value(this, () => "initial")
		.Validate(async (name, ct) => new[] { new ValidationResult($"original:{name}", new[] { nameof(Name) }) });
}
