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
		if (HasHandlers)
		{
			Enqueue(args);

			if (_dispatcher.HasThreadAccess)
			{
				Dequeue();
			}
			else
			{
				_dispatcher.TryEnqueue(Dequeue);
			}
		}
	}

	/// <inheritdoc />
	public void Invoke(Func<TArgs> args)
	{
		if (HasHandlers)
		{
			Enqueue(args());

			if (_dispatcher.HasThreadAccess)
			{
				Dequeue();
			}
			else
			{
				_dispatcher.TryEnqueue(Dequeue);
			}
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
		_handlers = null;
		HasHandlers = false;
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
