using System.Collections.Immutable;

namespace Uno.Extensions.Reactive.WinUI.Tests;

/// <summary>
/// MVUX Model whose <see cref="Items"/> feed delegates to
/// <see cref="HotReloadListFeedTarget.GetPipelineItems"/>. The target class is
/// a simple static class that HR can reliably patch (no source-generated
/// partials, no record synthesis). The source generator creates
/// <c>HotReloadListFeedViewModel</c> from this record.
/// </summary>
public partial record HotReloadListFeedModel
{
	public IListFeed<string> Items => ListFeed.Async(async ct => HotReloadListFeedTarget.GetPipelineItems());
}
