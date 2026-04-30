namespace TestHarness.Ext.Mvux;

public partial record MvuxStateModel
{
	public IState<string> Name => State.Value(this, () => "Initial Item");

	public IState<int> Counter => State.Value(this, () => 0);

	public async ValueTask IncrementCounter()
	{
		await Counter.UpdateAsync(c => c + 1, CancellationToken.None);
	}
}
