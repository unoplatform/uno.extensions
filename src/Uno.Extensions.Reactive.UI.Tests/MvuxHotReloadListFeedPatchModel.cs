using System.Collections.Immutable;

namespace Uno.Extensions.Reactive.WinUI.Tests;

// Hot-reload fixture for #3295 (spec 025): a list feed that is not a list state, patched on a live view model.
public partial record MvuxHotReloadListFeedPatchModel
{
	public IListFeed<string> Items => ListFeed.Async(async ct => ImmutableList.Create("original"));
}
