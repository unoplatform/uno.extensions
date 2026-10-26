namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadAddDerivedFeedModel
{
	public IState<int> Count => State.Value(this, () => 0);

	public async ValueTask Increment(CancellationToken ct) => await Count.UpdateAsync(c => c + 1, ct);

	// derived feed added here
}
