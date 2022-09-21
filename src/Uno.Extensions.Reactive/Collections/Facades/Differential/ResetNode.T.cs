using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which reset the collection
/// </summary>
internal sealed class ResetNode<T> : IDifferentialCollectionNode
{
	private readonly IImmutableList<T> _items;

	public ResetNode(IImmutableList<T> items)
	{
		_items = items;
	}

	/// <inheritdoc />
	public IDifferentialCollectionNode? Previous => null;

	/// <inheritdoc />
	public int Count => _items.Count;

	/// <inheritdoc />
	public object ElementAt(int index)
		=> _items[index]!;

	/// <inheritdoc />
	public int IndexOf(object? value, int startingAt, IEqualityComparer? comparer)
		=> _items.IndexOf((T)value!, startingAt, _items.Count - startingAt, comparer?.ToEqualityComparer<T>());
}
