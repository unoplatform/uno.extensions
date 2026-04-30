using System.Collections.Immutable;

namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxListFeedModel
{
	public IListFeed<MvuxItem> Items => ListFeed.Async(async ct =>
	{
		await Task.Delay(100, ct);
		return ImmutableList.Create(
			new MvuxItem("Apple", 1),
			new MvuxItem("Banana", 2),
			new MvuxItem("Cherry", 3)
		);
	});
}
