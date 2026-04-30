namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxStateModel
{
	public IState<string> Name => State.Value(this, () => "Initial Item");

	public IState<int> Counter => State.Value(this, () => 0);

	public async ValueTask IncrementCounter(CancellationToken ct)
	{
		await Counter.UpdateAsync(c => c + 1, ct);
	}
}
