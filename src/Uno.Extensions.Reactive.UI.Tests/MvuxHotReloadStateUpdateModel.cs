namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadStateUpdateModel
{
	public IState<string> CurrentValue => State.Async(this, async ct => "stateful");
}
