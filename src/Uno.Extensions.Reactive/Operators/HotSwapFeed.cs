using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Operators;

/// <summary>
/// This allows to dynamically change the source feed (cf. Remarks).
/// This is breaking all principles of feed and caching strategies.
/// It must not be used in any case, except for hot reload.
/// </summary>
/// <typeparam name="T">Type of the values of the feed.</typeparam>
/// <remarks>
/// WARNING: This feed **will not complete when the source completes**.
/// It will instead listen for a new source to enumerate.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class HotSwapFeed<T> : IFeed<T>
{
	private readonly object _gate = new();
	private ISignal<Message<T>>? _current;

	private event EventHandler<ISignal<Message<T>>?>? _currentChanged;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="feed">The original feed.</param>
	public HotSwapFeed(ISignal<Message<T>>? feed = null)
	{
		_current = feed;
	}

	/// <summary>
	/// The current source feed.
	/// </summary>
	public ISignal<Message<T>>? Current => _current;

	/// <summary>
	/// This allows to dynamically change the source feed (cf. Remarks)
	/// </summary>
	public void Set(ISignal<Message<T>>? feed)
	{
		lock (_gate)
		{
			if (_current == feed)
			{
				return;
			}

			_current = feed;
			_currentChanged?.Invoke(this, feed);
		}
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task, false positive: it's for the DisposeAsync which cannot be configured here
		await using var session = new Session(this, context, ct);
#pragma warning restore CA2007
		while (await session.MoveNextAsync().ConfigureAwait(false))
		{
			yield return session.Current;
		}
	}

	private class Session : IAsyncEnumerator<Message<T>>
	{
		private readonly HotSwapFeed<T> _owner;
		private readonly SourceContext _context;
		private readonly CancellationToken _ct;

		private TaskCompletionSource<SessionCurrentEnumerator>? _next = new();
		private SessionCurrentEnumerator _currentEnumerator;
		private bool _isFirstMessage = true; // Distinct from _isFirstMessageOfCurrentEnumerator by the fact that will forward it, no matter if it's an Initial or not.
		private bool _isFirstMessageOfCurrentEnumerator = true;

		public Session(HotSwapFeed<T> owner, SourceContext context, CancellationToken ct)
		{
			_owner = owner;
			_context = context;
			_ct = ct;

			_currentEnumerator = new SessionCurrentEnumerator(this, owner._current);
			_owner._currentChanged += OnFeedChanged;
		}

		public CancellationToken Token => _ct;

		/// <inheritdoc />
		public Message<T> Current { get; private set; } = Message<T>.Initial;

		/// <inheritdoc />
		public async ValueTask<bool> MoveNextAsync()
		{
			var next = _next;
			if (next is null) // Disposed
			{
				return false;
			}

			if (_currentEnumerator.GetEnumerator(_context) is { } enumerator)
			{
				var moveNext = enumerator.MoveNextAsync().AsTask();
				if (await Task.WhenAny(moveNext, next.Task).ConfigureAwait(false) == moveNext
					&& await moveNext.ConfigureAwait(false))
				{
					var canSkip = !_isFirstMessage && _isFirstMessageOfCurrentEnumerator;

					_isFirstMessageOfCurrentEnumerator = _isFirstMessage = false;

					if (canSkip)
					{
						if (Current.OverrideBy(enumerator.Current) is { Changes.Count: > 0 } current)
						{
							Current = current;
						}
						else
						{
							// The first message of the current enumerator is the same as the last of the previous enumerator, so we skip it
							return await MoveNextAsync().ConfigureAwait(false);
						}
					}
					else
					{
						Current = enumerator.Current;
					}

					return true;
				}
			}

			// Move to the next enumerator
			await _currentEnumerator.DisposeAsync().ConfigureAwait(false);
			_currentEnumerator = await next.Task.ConfigureAwait(false);
			_isFirstMessageOfCurrentEnumerator = true;

			// Then try again to move to the next message (using the new enumerator)
			return await MoveNextAsync().ConfigureAwait(false);
		}

		private void OnFeedChanged(object? sender, ISignal<Message<T>>? parent)
		{
			var next = _next;
			if (next is not null && Interlocked.CompareExchange(ref _next, new TaskCompletionSource<SessionCurrentEnumerator>(), next) == next)
			{
				next.TrySetResult(new SessionCurrentEnumerator(this, parent));
			}
			// else: disposed, nothing to do
		}

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			_owner._currentChanged -= OnFeedChanged;
			Interlocked.Exchange(ref _next, null)?.TrySetCanceled();
			await _currentEnumerator.DisposeAsync().ConfigureAwait(false);
		}
	}

	private record SessionCurrentEnumerator(Session Session, ISignal<Message<T>>? Feed) : IAsyncDisposable
	{
		private readonly CancellationTokenSource _ct = CancellationTokenSource.CreateLinkedTokenSource(Session.Token);
		private IAsyncEnumerator<Message<T>>? _enumerator;

		public IAsyncEnumerator<Message<T>>? GetEnumerator(SourceContext context)
			=> _enumerator ??= Feed is null ? null : context.GetOrCreateSource(Feed).GetAsyncEnumerator(_ct.Token);

		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			_ct.Cancel();
			if (_enumerator is not null)
			{
				try
				{
					await _enumerator.DisposeAsync().ConfigureAwait(false);
				}
				catch (NotSupportedException) { } // Seems to be the standard
				catch (ObjectDisposedException) { }
				catch (OperationCanceledException) { }
				catch (Exception error)
				{
					this.Log().Warn(error, "Failed to dispose current enumerator.");
				}
			}
			_ct.Dispose();
		}
	}
}
