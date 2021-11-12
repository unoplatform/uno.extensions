using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Utils.Logging;

namespace Uno.Extensions.Reactive;

internal sealed class ForEachRunner<T> : IForEachRunner
{
	private readonly Func<IAsyncEnumerable<T>> _enumeratorProvider;
	private readonly ActionAsync<T> _action;

	private CancellationTokenSource? _enumerationToken;

	public ForEachRunner(
		Func<IAsyncEnumerable<T>> enumeratorProvider,
		ActionAsync<T> action)
	{
		_enumeratorProvider = enumeratorProvider;
		_action = action;
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
			await _enumeratorProvider().ForEachAwaitAsync(async item => await _action(item, _enumerationToken.Token), _enumerationToken.Token);
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
