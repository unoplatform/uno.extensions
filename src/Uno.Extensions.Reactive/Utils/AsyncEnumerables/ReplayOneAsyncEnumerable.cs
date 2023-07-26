using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal class ReplayOneAsyncEnumerable<T> : IAsyncEnumerable<T>, IDisposable, IAsyncDisposable
{
	private readonly CancellationTokenSource _ct = new();
	private readonly IAsyncEnumerable<T> _inner;
	private readonly bool _isInitialSyncValuesSkippingAllowed;

	private Node _current = Node.Initial();
	private int _state = State.Idle;
	private Task? _enumeration; // Not used, only to keep ref

	private static class State
	{
		public const int Idle = 0;
		public const int Enumerating = 1;
		public const int Completed = 255;
		public const int Disposed = int.MaxValue;
	}

	public ReplayOneAsyncEnumerable(IAsyncEnumerable<T> inner, SubscriptionMode mode = SubscriptionMode.Lazy, bool isInitialSyncValuesSkippingAllowed = true)
	{
		_inner = inner;
		_isInitialSyncValuesSkippingAllowed = isInitialSyncValuesSkippingAllowed;

		if (mode.HasFlag(SubscriptionMode.Eager))
		{
			Enable();
		}
	}

	public void Enable() // a.k.a. Connect()
	{
		if (Interlocked.CompareExchange(ref _state, State.Enumerating, State.Idle) == State.Idle)
		{
			_enumeration = Enumerate(_inner, _ct.Token);
		}
	}

	/// <summary>
	/// Gets the last value enumerated from the source.
	/// This IS NOT the current value of an enumerator from <see cref="GetAsyncEnumerator"/>.
	/// </summary>
	public bool TryGetCurrent([NotNullWhen(true)] out T value)
		=> _current.TryGetValue(out value);

	public void Disable()
	{
		// Not supported yet
	}

	private async Task Enumerate(IAsyncEnumerable<T> source, CancellationToken ct)
	{
		try
		{
			await foreach (var value in source.WithCancellation(ct).ConfigureAwait(false))
			{
				if (!MoveNext(Node.Next(value), ct))
				{
					break;
				}
			}
		}
		finally
		{
			Interlocked.CompareExchange(ref _state, State.Enumerating, State.Completed);
			_current.TrySetNext(Node.Final());
		}
	}

	private bool MoveNext(Node next, CancellationToken ct)
	{
		while (true)
		{
			var current = _current;
			if (ct.IsCancellationRequested)
			{
				// Update has been aborted or State has been disposed
				return false;
			}

			if (Interlocked.CompareExchange(ref _current, next, current) == current)
			{
				current.TrySetNext(next);
				return true;
			}
		}
	}

	/// <inheritdoc />
	public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct)
	{
		try
		{
			var needsToEnableEnumeration = true;
			if (_isInitialSyncValuesSkippingAllowed)
			{
				Enable();
				needsToEnableEnumeration = false;
			}

			var current = _current;
			do
			{
				if (current.TryGetValue(out var value))
				{
					yield return value;
				}

				if (needsToEnableEnumeration)
				{
					var next = current.GetNext(ct);
					Enable();
					needsToEnableEnumeration = false;
					current = await next.ConfigureAwait(false);
				}
				else
				{
					current = await current.GetNext(ct).ConfigureAwait(false);
				}
			} while (current is not null && !ct.IsCancellationRequested);
		}
		finally
		{
			Disable();
		}
	}

	private class Node
	{
		private readonly bool _hasValue;
		private readonly T? _value;
		private readonly TaskCompletionSource<Node>? _next;

		public static Node Initial() => new(false, default, new TaskCompletionSource<Node>(TaskCreationOptions.None)); // Do not use AttachToParent to avoid leak of the current context which creates the initial task 
		public static Node Final() => new(false, default, null);

		public static Node Next(T value) => new(true, value, new TaskCompletionSource<Node>(TaskCreationOptions.None)); // Do not use AttachToParent to avoid leak of the current context which gives only the current value 

		private Node(bool hasValue, T? value, TaskCompletionSource<Node>? next)
		{
			_hasValue = hasValue;
			_value = value;
			_next = next;
		}

		public bool TryGetValue([NotNullWhen(true)] out T value)
		{
			value = _value!;
			return _hasValue;
		}

		public void TrySetNext(Node node)
			=> (_next ?? throw new InvalidOperationException("Cannot move next on final node.")).TrySetResult(node);

		public async ValueTask<Node?> GetNext(CancellationToken ct)
			=> _next is null ? default : await _next.Task.ConfigureAwait(false);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_state = State.Disposed;
		_current.TrySetNext(Node.Final());
		_ct.Cancel();

		_enumeration = null; // Release ref
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
		=> Dispose();
}
