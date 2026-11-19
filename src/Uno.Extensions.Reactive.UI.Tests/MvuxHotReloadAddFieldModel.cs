namespace Uno.Extensions.Reactive.WinUI.Tests;

// Hot-reload fixture for dotnet/roslyn#86338 (#3295): the delta adds a field to the model and a member to its view model,
// so the view model ctor is updated in place while its public ctor keeps creating the original model.
public partial record MvuxHotReloadAddFieldModel
{
	public IState<int> Count => State.Value(this, () => 0);

	public async ValueTask Increment(CancellationToken ct) => await Count.UpdateAsync(c => c + 1, ct);

	// field-backed state added here
}
