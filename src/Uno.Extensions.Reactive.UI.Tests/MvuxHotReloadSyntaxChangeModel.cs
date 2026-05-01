namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadSyntaxChangeModel
{
	public IFeed<string> CurrentValue => Feed.Async(async ct => "round-trip");
}
