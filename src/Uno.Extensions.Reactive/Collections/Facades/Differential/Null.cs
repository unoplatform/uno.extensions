using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which does nothing
/// </summary>
internal sealed class Null : IDifferentialCollectionNode
{
	private readonly IDifferentialCollectionNode _previous;

	public Null(IDifferentialCollectionNode previous) => _previous = previous;

	/// <inheritdoc />
	public int Count => _previous.Count;

	/// <inheritdoc />
	public object? ElementAt(int index)
		=> _previous.ElementAt(index);

	/// <inheritdoc />
	public int IndexOf(object? element, int startingAt, IEqualityComparer? comparer)
		=> _previous.IndexOf(element, startingAt, comparer);
}
