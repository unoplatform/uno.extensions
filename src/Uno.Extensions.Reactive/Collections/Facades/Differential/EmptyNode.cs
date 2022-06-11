using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// A node of a linked stack of <see cref="IDifferentialCollectionNode"/> which is clears the collection
/// </summary>
internal sealed class EmptyNode : IDifferentialCollectionNode
{
	/// <inheritdoc />
	public IDifferentialCollectionNode? Previous => null;

	/// <inheritdoc />
	public int Count => 0;

	/// <inheritdoc />
	public object? ElementAt(int index)
		=> throw new IndexOutOfRangeException();

	/// <inheritdoc />
	public int IndexOf(object? element, int startingAt, IEqualityComparer? comparer = null)
		=> -1;
}
