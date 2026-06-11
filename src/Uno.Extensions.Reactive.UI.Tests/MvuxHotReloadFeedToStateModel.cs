namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadFeedToStateModel
{
	public IState<string> CurrentValue => State.Async(this, async ct => "convertible");
}
