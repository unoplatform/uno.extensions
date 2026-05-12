using System.Collections.Immutable;

namespace Uno.Extensions.Reactive.WinUI.Tests;

/// <summary>
/// Target class for hot-reload ListFeed tests. Method bodies here are modified
/// at runtime by <see cref="Given_HotReloadListFeed"/> via HotReloadHelper.
/// <para>
/// HR on simple static classes works reliably (no source-generated partials,
/// no record synthesis). The MVUX Model delegates to <see cref="GetPipelineItems"/>
/// so the feed test exercises the HR delta through the full MVUX pipeline.
/// </para>
/// </summary>
internal static class HotReloadListFeedTarget
{
	/// <summary>Used by the basic direct-call HR test.</summary>
	internal static IImmutableList<string> GetItems()
	{
		return ImmutableList.Create("Item1", "Item2", "Item3");
	}

	/// <summary>Used by the MVUX pipeline HR test via <c>HotReloadListFeedModel</c>.</summary>
	internal static IImmutableList<string> GetPipelineItems()
	{
		return ImmutableList.Create("PipeA", "PipeB", "PipeC");
	}
}
