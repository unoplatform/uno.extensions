using System;
using System.Collections;
using System.Linq;
using Uno.Extensions.Reactive.Utils;

namespace Umbrella.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which reset the collection
/// </summary>
public sealed class Reset : IDifferentialCollectionNode
{
	private readonly IList _items;

	public Reset(IList items)
	{
		_items = items;
	}

	/// <inheritdoc />
	public int Count => _items.Count;

	/// <inheritdoc />
	public object ElementAt(int index)
		=> _items[index];

	/// <inheritdoc />
	public int IndexOf(object? value, int startingAt, IEqualityComparer? comparer)
		=> _items.IndexOf(value, startingAt, comparer);
}
