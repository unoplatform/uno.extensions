using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Events;

internal class EventManager<THandler, TArgs>
	where TArgs : class
{
	private readonly object _owner;
	private readonly Func<THandler, Action<object, TArgs>> _raiseMethod;
	private readonly bool _isCoalescable;

	private readonly DispatcherLocal<IInvocationList<THandler, TArgs>> _invocationLists;

	public EventManager(object owner, Func<THandler, Action<object, TArgs>> raiseMethod, bool isCoalescable = false, Func<DispatcherQueue?>? schedulersProvider = null)
	{
		_owner = owner;
		_raiseMethod = raiseMethod;
		_isCoalescable = isCoalescable;

		_invocationLists = new(CreateInvocationList, schedulersProvider);
	}

	///// <inheritdoc />
	//public bool HasHandlers { get; }

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
		foreach (var list in _invocationLists.GetValues())
		{
			list.value.Invoke(args);
		}
	}

	/// <inheritdoc />
	public void Raise(Func<TArgs> args)
	{
		foreach (var list in _invocationLists.GetValues())
		{
			list.value.Invoke(args);
		}
	}

	private IInvocationList<THandler, TArgs> CreateInvocationList(DispatcherQueue? dispatcher)
		=> (dispatcher, _isCoalescable) switch
		{
			(not null, true) => new CoalescingDispatcherInvocationList<THandler, TArgs>(_owner, _raiseMethod, dispatcher),
			(not null, false) => new QueueingDispatcherInvocationList<THandler, TArgs>(_owner, _raiseMethod, dispatcher),
			_ => new MultiThreadInvocationList<THandler, TArgs>(_owner, _raiseMethod)
		};

	/// <inheritdoc />
	public void Dispose()
	{
		foreach (var list in _invocationLists.GetValues())
		{
			list.value.Dispose();
		}
	}
}
