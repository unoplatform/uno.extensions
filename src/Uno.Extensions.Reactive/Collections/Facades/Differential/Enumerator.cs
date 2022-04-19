using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// An <see cref="IEnumerator"/> for differential collections
/// </summary>
internal sealed class Enumerator : IEnumerator, IEnumerator<object?>
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
			Current = _head.ElementAt(_index);
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
	public object? Current { get; private set; }

	/// <inheritdoc />
	public void Dispose()
	{
	}
}
