using System.Collections.Immutable;

namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadListFeedRemoveModel
{
	public IListFeed<string> Items => ListFeed.Async(async ct => ImmutableList.Create("one", "two", "three"));
}
