using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Umbrella.Feeds.Collections.Facades
{
	// TODO: Uno : use enumerators!

	public class CompositeEnumerator : IEnumerator
	{
		private readonly IEnumerator[] _inners;
		private int _current;

		public CompositeEnumerator(IEnumerable<IEnumerable> enumerables)
			: this(enumerables.Select(i => i.GetEnumerator()).ToArray())
		{
		}

		public CompositeEnumerator(params IEnumerator[] inners)
		{
			_inners = inners ?? throw new ArgumentNullException(nameof(inners), "The inners must not be null.");
		}

		public object? Current { get; private set; }

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

			Current = default;
			return false;
		}

		public void Reset()
		{
			for (var i = 0; i <= Math.Min(_current, _inners.Length - 1); i++)
			{
				_inners[i].Reset();
			}

			_current = 0;
			Current = default;
		}
	}
}
