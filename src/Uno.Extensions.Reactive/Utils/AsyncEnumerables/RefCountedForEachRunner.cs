using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Utils;

/// <summary>
/// An enumerator which execute an <see cref="AsyncAction{T}"/> for each items of an <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
/// <typeparam name="T">The type of items</typeparam>
internal sealed class RefCountedForEachRunner<T> : IForEachRunner
{
	private readonly Func<IAsyncEnumerable<T>> _enumeratorProvider;
	private readonly AsyncAction<T> _asyncAction;

	private readonly object _enumerationGate = new();
	private CancellationTokenSource? _enumerationToken;
	private Task? _enumeration;

	private int _refCount;
	private int _status;

	private static class Status
	{
		public const int Idle = 0; // Enumeration was not started yet, or the it was stopped since last ref has been removed.
		public const int Enumerating = 1;
		public const int Completed = 2; // We reach the end of the source enumerable, we won't retry to enumerate the source.
		public const int Disposed = 255;
	}

	public RefCountedForEachRunner(
		Func<IAsyncEnumerable<T>> enumeratorProvider, 
		AsyncAction<T> asyncAction)
	{
		_enumeratorProvider = enumeratorProvider;
		_asyncAction = asyncAction;
	}

	/// <summary>
	/// Start enumeration (if not yet started) without increasing the reference counter.
	/// </summary>
	public void Prefetch()
		=> EnsureEnumeration();

	/// <summary>
	/// Starts enumeration (if not yet started) and increase the reference counter,
	/// so enumeration won't be stopped until its goes back to 0. 
	/// </summary>
	/// <returns>A disposable which decrease the reference counter when disposed.</returns>
	public IDisposable Enable()
	{
		CheckDisposed();

		return new Subscription(this);
	}

	private void AddRef()
	{
		if (Interlocked.Increment(ref _refCount) == 1)
		{
			EnsureEnumeration();
		}
	}

	private void RemoveRef()
	{
		if (Interlocked.Decrement(ref _refCount) == 0)
		{
			lock (_enumerationGate)
			{
				if (_refCount == 0)
				{
					AbortEnumeration();
				}
			}
		}
	}

	private void EnsureEnumeration()
	{
		lock (_enumerationGate)
		{
			if (_enumerationToken is not null)
			{
				return; // Either Disposed or Prefetch-ed
			}

			var cts = new CancellationTokenSource();

			_enumerationToken = cts;
			_enumeration = Task.Run(() => EnumerateInner(cts.Token), cts.Token);
		}
	}

	private void AbortEnumeration()
	{
		lock (_enumerationGate)
		{
			Interlocked.Exchange(ref _enumerationToken, null)?.Cancel();
		}
	}

	private async Task EnumerateInner(CancellationToken ct)
	{
		if (Interlocked.CompareExchange(ref _status, Status.Enumerating, Status.Idle) != Status.Idle)
		{
			return;
		}

		try
		{
			await _enumeratorProvider().ForEachAwaitAsync(async item => await _asyncAction(item, ct).ConfigureAwait(false), ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception error)
		{
			this.Log().Error(error, "Enumeration of inner source failed. The current State is no longer in sync with the inner feed and won't be.");
		}
		finally
		{
			Interlocked.CompareExchange(ref _status, ct.IsCancellationRequested ? Status.Idle : Status.Completed, Status.Enumerating);
		}
	}

	private void CheckDisposed()
	{
		if (_status is Status.Disposed)
		{
			throw new ObjectDisposedException(nameof(RefCountedForEachRunner<T>));
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_status = Status.Disposed;
		_enumerationToken?.Cancel(); // Do not set it to 'null', so we cannot restart enumeration
	}

	private class Subscription : IDisposable
	{
		private readonly RefCountedForEachRunner<T> _src;
		private int _isDisposed = 0;

		public Subscription(RefCountedForEachRunner<T> src)
		{
			_src = src;
			src.AddRef();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
			{
				_src.RemoveRef();
			}

			GC.SuppressFinalize(this);
		}

		~Subscription()
		{
			Dispose();
		}
	}
}
