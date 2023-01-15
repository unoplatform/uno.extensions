using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive.Utils;

internal class DeferredAsyncEnumerable<T> : IAsyncEnumerable<T>
{
	private readonly IAsyncEnumerable<T> _inner;

	public DeferredAsyncEnumerable(IAsyncEnumerable<T> inner)
	{
		_inner = inner;
	}

	/// <inheritdoc />
	public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default)
		=> new DeferredAsyncEnumerator<T>(_inner.GetAsyncEnumerator(ct));
}
