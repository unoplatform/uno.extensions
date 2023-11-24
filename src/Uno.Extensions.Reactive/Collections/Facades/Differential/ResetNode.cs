using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which reset the collection
/// </summary>
internal sealed class ResetNode : IDifferentialCollectionNode
{
	private readonly IList _items;

	public ResetNode(IList items)
	{
		_items = items;
	}

	/// <inheritdoc />
	public IDifferentialCollectionNode? Previous => null;

	/// <inheritdoc />
	public int Count => _items.Count;

	/// <inheritdoc />
	public object? ElementAt(int index)
		=> _items[index];

	/// <inheritdoc />
	public int IndexOf(object? value, int startingAt, IEqualityComparer? comparer)
		=> _items.IndexOf(value, startingAt, comparer);
}
