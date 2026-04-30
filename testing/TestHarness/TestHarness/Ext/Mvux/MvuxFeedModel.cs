namespace TestHarness.Ext.Mvux;

public partial record MvuxFeedModel
{
	public IFeed<MvuxItem> CurrentItem => Feed.Async(async ct =>
	{
		await Task.Delay(500, ct);
		return new MvuxItem("Test Item", 1);
	});
}
