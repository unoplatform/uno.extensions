using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Umbrella.Collections.Facades.Differential;

/// <summary>
/// An <see cref="IEnumerator"/> for differential collections
/// </summary>
internal sealed class Enumerator<T> : IEnumerator<T>
{
	private readonly IDifferentialCollectionNode _head;
	private int _index = -1;

	public Enumerator(IDifferentialCollectionNode head)
		=> _head = head;

	/// <inheritdoc />
	public bool MoveNext()
	{
		if (++_index < _head.Count)
		{
			Current = (T)_head.ElementAt(_index)!; // ! => The node only wraps the T which either is nullable, either didn't permitted the add
			return true;
		}

		else
		{
			Current = default!;
			return false;
		}
	}

	/// <inheritdoc />
	public void Reset() => _index = -1;

	/// <inheritdoc />
	public T Current { get; private set; } = default!;
	object? IEnumerator.Current => Current;

	/// <inheritdoc />
	public void Dispose()
	{
	}
}
