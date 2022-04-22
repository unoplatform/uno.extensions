using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Composite;


// TODO: Uno : use enumerators!
internal class CompositeEnumerator<T> : IEnumerator<T>
{
	private readonly IEnumerator<T>[] _inners;
	private int _current;

	public CompositeEnumerator(IEnumerable<IEnumerable<T>> enumerables)
		: this(enumerables.Select(i => i.GetEnumerator()).ToArray())
	{
	}

	public CompositeEnumerator(params IEnumerator<T>[] inners)
	{
		_inners = inners ?? Array.Empty<IEnumerator<T>>();
	}

	object? IEnumerator.Current => Current;
	public T Current { get; private set; } = default!;

	public bool MoveNext()
	{
		while (_current < _inners.Length)
		{
			if (_inners[_current].MoveNext())
			{
				Current = _inners[_current].Current;
				return true;
			}
			else
			{
				_current++;
			}
		}

		Current = default!;
		return false;
	}

	public void Reset()
	{
		for (var i = 0; i <= Math.Min(_current, _inners.Length - 1); i++)
		{
			_inners[i].Reset();
		}

		_current = 0;
		Current = default!;
	}

	public void Dispose()
	{
		for (var i = 0; i <= Math.Min(_current, _inners.Length - 1); i++)
		{
			_inners[i].Dispose();
		}
	}
}
