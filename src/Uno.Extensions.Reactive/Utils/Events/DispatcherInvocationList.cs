using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Events;

internal abstract class DispatcherInvocationList<THandler, TArgs> : IInvocationList<THandler, TArgs>
	where TArgs : class
{
	private readonly object _owner;
	private readonly Func<THandler, Action<object, TArgs>> _raise;
	private readonly IDispatcher _dispatcher;

	private List<THandler>? _handlers = new();
	private bool _isEnumeratingHandlers;
	private int _queueLength;
	private bool _isDisposed;

	public DispatcherInvocationList(object owner, Func<THandler, Action<object, TArgs>> raiseMethod, IDispatcher dispatcher)
	{
		_owner = owner;
		_raise = raiseMethod;
		_dispatcher = dispatcher;
	}

	/// <inheritdoc />
	public bool HasHandlers { get; private set; }

	/// <inheritdoc />
	public void Add(THandler handler)
	{
		if (GetHandlersForWrite() is { } handlers)
		{
			handlers.Add(handler);
			HasHandlers = _handlers is not null; // Checks if disposed while adding item
		}
	}

	/// <inheritdoc />
	public void Remove(THandler handler)
	{
		if (GetHandlersForWrite() is { } handlers
			&& handlers.Remove(handler))
		{
			HasHandlers = handlers.Count > 0 && _handlers is not null; // Checks if disposed while adding item
		}
	}

	/// <inheritdoc />
	public void Invoke(TArgs args)
	{
		if (_isDisposed || !HasHandlers)
		{
			return;
		}

		Enqueue(args);

		if (_dispatcher.HasThreadAccess)
		{
			Dequeue();
		}
		else if (_dispatcher.TryEnqueue(InvokeAsync))
		{
			Interlocked.Increment(ref _queueLength);
		}
	}

	/// <inheritdoc />
	public void Invoke(Func<TArgs> args)
	{
		if (_isDisposed || !HasHandlers)
		{
			return;
		}

		Enqueue(args());

		if (_dispatcher.HasThreadAccess)
		{
			Dequeue();
		}
		else if (_dispatcher.TryEnqueue(InvokeAsync))
		{
			Interlocked.Increment(ref _queueLength);
		}
	}

	private void InvokeAsync()
	{
		Interlocked.Decrement(ref _queueLength);
		Dequeue();

		if (_isDisposed && _queueLength is 0)
		{
			Dispose();
		}
	}

	protected abstract void Enqueue(TArgs args);
	protected abstract void Dequeue();

	protected bool RaiseCore(TArgs? args)
	{
		var handlers = _handlers;
		if (args is null || handlers is null)
		{
			return false;
		}

		try
		{
			_isEnumeratingHandlers = true;
			foreach (var handler in handlers)
			{
				_raise(handler!)(_owner, args);
			}
		}
		finally
		{
			_isEnumeratingHandlers = false;
		}

		return true;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		// Flag as disposed. Doing this prevents queueing new events, but still allows to add/remove handlers.
		// This is to allow the case where an handler un-register and re-register itself.
		_isDisposed = true;

		if (_queueLength is 0)
		{
			// If there are some pending events, we defer the actual dispose after the dispatcher dequeued them.
			// We will clear the handlers list when the last event is dequeued (re-invoke this Dispose method).
			_handlers = null;
			HasHandlers = false;
		}
	}

	private List<THandler>? GetHandlersForWrite()
	{
		if (_isEnumeratingHandlers && _handlers is { } handlers)
		{
			// Even if this method is expected to be invoked on the UI thread,
			// we use interlocked here to avoir concurrency issue with Dispose
			Interlocked.CompareExchange(ref _handlers, new List<THandler>(handlers), handlers);
			_isEnumeratingHandlers = false;

			return _handlers;
		}
		else
		{
			return _handlers;
		}
	}
}
