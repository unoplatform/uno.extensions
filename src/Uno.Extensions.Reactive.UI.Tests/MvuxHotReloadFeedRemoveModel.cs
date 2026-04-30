namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadFeedRemoveModel
{
	public IFeed<string> CurrentValue => Feed.Async(async ct => "hello");
}
