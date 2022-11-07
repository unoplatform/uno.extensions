using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal class DeferredAsyncEnumerator<T> : IAsyncEnumerator<T>
{
	private readonly IAsyncEnumerator<T> _inner;

	public DeferredAsyncEnumerator(IAsyncEnumerator<T> inner)
	{
		_inner = inner;
	}

	/// <inheritdoc />
	public T Current => _inner.Current;

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
		=> await Task.Run(() => _inner.DisposeAsync().AsTask()).ConfigureAwait(true);

	/// <inheritdoc />
	public async ValueTask<bool> MoveNextAsync()
		=> await Task.Run(() => _inner.MoveNextAsync().AsTask()).ConfigureAwait(true);
}
