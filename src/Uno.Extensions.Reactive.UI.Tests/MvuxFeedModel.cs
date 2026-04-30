namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxFeedModel
{
	public IFeed<MvuxItem> CurrentItem => Feed.Async(async ct =>
	{
		await Task.Delay(100, ct);
		return new MvuxItem("Test Item", 1);
	});
}
