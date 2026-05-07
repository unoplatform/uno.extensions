using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions.Reactive.Core.HotReload;

/// <summary>
/// Internal registry populated by the Reactive source generator. Maps a (model type, member name)
/// pair to the set of types whose IL the corresponding feed/state lambda calls into. Used by
/// <see cref="Sources.AsyncFeed{T}"/> to decide whether a hot-reload metadata update should
/// re-fire the feed (e.g. `Feed.Async(async ct => Helper.Get())` should refresh when Helper changes,
/// even though Helper is not the lambda's declaring type).
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class FeedDependencyRegistry
{
	private static readonly ConcurrentDictionary<(Type Owner, string Member), Type[]> _entries = new();

	/// <summary>
	/// Called from the source-generated module initializer. Do not call from user code.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void Register(Type owner, string memberName, params Type[] dependencies)
		=> _entries[(owner, memberName)] = dependencies;

	[RequiresUnreferencedCode("`MetadataUpdateOriginalTypeAttribute` may be a per-assembly type, so it cannot be statically known.")]
	internal static Type[]? Resolve(Type? owner, string memberName)
	{
		if (owner is null || string.IsNullOrEmpty(memberName))
		{
			return null;
		}

		while (owner.DeclaringType is { } d)
		{
			owner = d;
		}

		// If the AsyncFeed was constructed AFTER an HR delta, `owner` may already be a shadow generation
		// (e.g. Foo#3) — but the module-initializer registered entries against the ORIGINAL type. Resolve
		// back via MetadataUpdateOriginalTypeAttribute so the lookup hits.
		owner = HotReloadService.GetOriginalType(owner) ?? owner;

		return _entries.TryGetValue((owner, memberName), out var deps) ? deps : null;
	}
}
