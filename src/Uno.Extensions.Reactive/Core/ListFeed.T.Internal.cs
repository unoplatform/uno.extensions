using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Collections;

namespace Uno.Extensions.Reactive;

static partial class ListFeed<T>
{
	/// <summary>
	/// The default comparer to use in feed when not configured by user.
	/// </summary>
	internal static ItemComparer<T> DefaultComparer { get; } = KeyEqualityComparer.Find<T>() is {} keyComparer
		? new (keyComparer, EqualityComparer<T>.Default)
		: ItemComparer<T>.Default; // No comparers, use default behavior of the list

	internal static ItemComparer<T> GetComparer(ItemComparer<T> comparer)
		=> comparer is { IsNull: true } ? DefaultComparer : comparer;

	/// <summary>
	/// The default analyzer that should be used in feeds that should not offer a way to customize the <see cref="ItemComparer{T}"/>,
	/// like all operators.
	/// </summary>
	internal static CollectionAnalyzer<T> DefaultAnalyzer { get; } = new(DefaultComparer);

	internal static CollectionAnalyzer<T> GetAnalyzer(ItemComparer<T> comparer)
		=> comparer is { IsNull: true } ? DefaultAnalyzer : new(GetComparer(comparer));
}
