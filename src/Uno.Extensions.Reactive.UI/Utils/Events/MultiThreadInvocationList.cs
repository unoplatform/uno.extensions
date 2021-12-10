using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Events;

internal sealed class MultiThreadInvocationList<THandler, TArgs> : IInvocationList<THandler, TArgs>
{
	private readonly object _gate = new();

	private readonly object _owner;
	private readonly Func<THandler, Action<object, TArgs>> _raiseMethod;

	private List<THandler>? _handlers = new();

	public MultiThreadInvocationList(object owner, Func<THandler, Action<object, TArgs>> raiseMethod)
	{
		_owner = owner;
		_raiseMethod = raiseMethod;
	}

	/// <inheritdoc />
	public bool HasHandlers { get; private set; }

	/// <inheritdoc />
	public void Add(THandler handler)
	{
		lock (_gate)
		{
			_handlers?.Add(handler);
			HasHandlers = true;
		}
	}

	/// <inheritdoc />
	public void Remove(THandler handler)
	{
		lock (_gate)
		{
			if (_handlers?.Remove(handler) ?? false)
			{
				HasHandlers = _handlers.Count > 0;
			}
		}
	}

	/// <inheritdoc />
	public void Invoke(Func<TArgs> args)
	{
		if (HasHandlers)
		{
			RaiseCore(args());
		}
	}

	/// <inheritdoc />
	public void Invoke(TArgs args)
	{
		if (HasHandlers)
		{
			RaiseCore(args);
		}
	}

	public void RaiseCore(TArgs args)
	{
		List<THandler>? handlers;
		lock (_gate)
		{
			handlers = _handlers?.ToList();
		}

		if (handlers is { Count: > 0 })
		{
			foreach (var handler in handlers)
			{
				_raiseMethod(handler)(_owner, args);
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		lock (_gate)
		{
			_handlers = null;
		}
	}
}
