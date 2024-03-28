using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// An index based range of selected items.
/// </summary>
/// <param name="FirstIndex">The index of the first selected item.</param>
/// <param name="Length">The count of selected items.</param>
public sealed record SelectionIndexRange(uint FirstIndex, uint Length)
{
	/// <summary>
	/// Gets the index of the last selected item.
	/// </summary>
	public uint LastIndex => Length is 0 ? FirstIndex : FirstIndex + Length - 1;

	/// <inheritdoc />
	public override string ToString()
		=> Length is 0
			? "--Empty--"
			: $"[{FirstIndex}, {LastIndex}]";
}
