using System.Collections.Immutable;

namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadMultiModel
{
	public IFeed<string> Title => Feed.Async(async ct => "title");
	public IListFeed<string> Items => ListFeed.Async(async ct => ImmutableList.Create("a", "b", "c"));
}
