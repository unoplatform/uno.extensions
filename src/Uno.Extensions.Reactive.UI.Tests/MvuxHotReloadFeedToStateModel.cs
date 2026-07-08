namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadFeedToStateModel
{
	public IFeed<string> CurrentValue => Feed.Async(async ct => "convertible");
}
