using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which does nothing
/// </summary>
internal sealed class NullNode : IDifferentialCollectionNode
{
	public NullNode(IDifferentialCollectionNode previous)
		=> Previous = previous;

	/// <inheritdoc />
	public IDifferentialCollectionNode Previous { get; }

	/// <inheritdoc />
	public int Count => Previous.Count;

	/// <inheritdoc />
	public object? ElementAt(int index)
		=> Previous.ElementAt(index);

	/// <inheritdoc />
	public int IndexOf(object? element, int startingAt, IEqualityComparer? comparer)
		=> Previous.IndexOf(element, startingAt, comparer);
}
