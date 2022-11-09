using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal class ConditionalDeferredAsyncEnumerator<T> : IAsyncEnumerator<T>
{
	private readonly IAsyncEnumerator<T> _inner;
	private readonly Func<bool> _deferringCondition;

	public ConditionalDeferredAsyncEnumerator(IAsyncEnumerator<T> inner, Func<bool> deferringCondition)
	{
		_inner = inner;
		_deferringCondition = deferringCondition;
	}

	/// <inheritdoc />
	public T Current => _inner.Current;

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_deferringCondition())
		{
			await Task.Run(() => _inner.DisposeAsync().AsTask()).ConfigureAwait(true);
		}
		else
		{
			await _inner.DisposeAsync().ConfigureAwait(false);
		}
	}

	/// <inheritdoc />
	public async ValueTask<bool> MoveNextAsync()
	{
		if (_deferringCondition())
		{
			return await Task.Run(() => _inner.MoveNextAsync().AsTask()).ConfigureAwait(true);
		}
		else
		{
			return await _inner.MoveNextAsync().ConfigureAwait(false);
		}
	}
}
