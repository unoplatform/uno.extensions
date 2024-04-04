using System;
using System.Linq;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Events;

internal class EventManager<THandler, TArgs>
	where TArgs : class
{
	private readonly object _owner;
	private readonly Func<THandler, Action<object, TArgs>> _raiseMethod;
	private readonly bool _isCoalescable;
	private readonly bool _allowBgThread;

	private readonly DispatcherLocal<IInvocationList<THandler, TArgs>> _invocationLists;

	public EventManager(
		object owner,
		Func<THandler, Action<object, TArgs>> raiseMethod,
		bool isCoalescable = false,
		bool isBgThreadAllowed = true,
		FindDispatcher? schedulersProvider = null)
	{
		_owner = owner;
		_raiseMethod = raiseMethod;
		_isCoalescable = isCoalescable;
		_allowBgThread = isBgThreadAllowed;

		_invocationLists = new(CreateInvocationList, schedulersProvider);
	}

	/// <inheritdoc />
	public void Add(THandler? handler)
	{
		if (handler is not null)
		{
			_invocationLists.Value.Add(handler);
		}
	}

	/// <inheritdoc />
	public void Remove(THandler? handler)
	{
		if (handler is not null)
		{
			_invocationLists.Value.Remove(handler);
		}
	}

	/// <inheritdoc />
	public void Raise(TArgs args)
	{
		// Note: We prefer to use the GetValues instead of the ForEachValueAsync as we don't mind to miss a new handler
		//		 inserted during enumeration, and the handler might be heavy and would lock the DispatcherLocal for too long.
		foreach (var list in _invocationLists.GetValues())
		{
			list.value.Invoke(args);
		}
	}

	/// <inheritdoc />
	public void Raise(Func<TArgs> args)
	{
		// Note: We prefer to use the GetValues instead of the ForEachValueAsync as we don't mind to miss a new handler
		//		 inserted during enumeration, and the handler might be heavy and would lock the DispatcherLocal for too long.
		foreach (var list in _invocationLists.GetValues())
		{
			list.value.Invoke(args);
		}
	}

	private IInvocationList<THandler, TArgs> CreateInvocationList(IDispatcher? dispatcher)
		=> dispatcher switch
		{
			not null when _isCoalescable => new CoalescingDispatcherInvocationList<THandler, TArgs>(_owner, _raiseMethod, dispatcher),
			not null => new QueueingDispatcherInvocationList<THandler, TArgs>(_owner, _raiseMethod, dispatcher),
			null when _allowBgThread => new MultiThreadInvocationList<THandler, TArgs>(_owner, _raiseMethod),
			null => throw new InvalidOperationException("Cannot register an event handler from a background thread.")
		};

	/// <inheritdoc />
	public void Dispose()
		=> _invocationLists.ForEachValue(static (_, list) => list.Dispose());
}
