using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Utils;

internal sealed class ForEachRunner<T> : IForEachRunner
{
	private readonly Func<IAsyncEnumerable<T>> _enumeratorProvider;
	private readonly AsyncAction<T> _asyncAction;

	private CancellationTokenSource? _enumerationToken;

	public ForEachRunner(
		Func<IAsyncEnumerable<T>> enumeratorProvider,
		AsyncAction<T> asyncAction)
	{
		_enumeratorProvider = enumeratorProvider;
		_asyncAction = asyncAction;
	}

	/// <inheritdoc />
	public void Prefetch()
		=> EnsureEnumeration();

	/// <inheritdoc />
	public IDisposable? Enable()
	{
		EnsureEnumeration();

		return null;
	}

	private async void EnsureEnumeration()
	{
		if (_enumerationToken is not null || Interlocked.CompareExchange(ref _enumerationToken, new CancellationTokenSource(), null) != null)
		{
			return;
		}

		try
		{
			await _enumeratorProvider().ForEachAwaitAsync(async item => await _asyncAction(item, _enumerationToken.Token).ConfigureAwait(false), _enumerationToken.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception error)
		{
			this.Log().Error(error, "Enumeration of inner source failed. The current State is no longer in sync with the inner feed and won't be.");
		}
	}

	/// <inheritdoc />
	public void Dispose()
		=> _enumerationToken?.Cancel();
}
