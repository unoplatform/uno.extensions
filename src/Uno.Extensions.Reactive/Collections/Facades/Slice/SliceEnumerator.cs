using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Slice;

internal class SliceEnumerator : IEnumerator
{
	private readonly IEnumerator _inner;
	private readonly int _start;
	private readonly int _end;

	private int _nextIndex;

	public SliceEnumerator(IEnumerator inner, int index, int count)
	{
		_inner = inner;
		_start = index;
		_end = index + count - 1;
	}

	/// <inheritdoc />
	public object? Current => _inner.Current;

	/// <inheritdoc />
	public bool MoveNext()
	{
		while (_nextIndex < _start && _inner.MoveNext())
		{
			_nextIndex++;
		}

		if (_nextIndex < _start || _nextIndex > _end)
		{
			return false;
		}

		if (MoveNext())
		{
			_nextIndex++;
			return true;
		}
		else
		{
			_nextIndex = int.MaxValue;
			return false;
		}
	}

	/// <inheritdoc />
	public void Reset()
	{
		_inner.Reset();
		_nextIndex = 0;
	}
}
