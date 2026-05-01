namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadModel
{
	public IFeed<string> CurrentValue => Feed.Async(async ct => MvuxHotReloadTarget.GetValue());
}
