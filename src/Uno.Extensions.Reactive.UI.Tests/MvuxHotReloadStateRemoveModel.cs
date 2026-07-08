namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadStateRemoveModel
{
	public IState<string> CurrentValue => State.Async(this, async ct => "stateful");
}
