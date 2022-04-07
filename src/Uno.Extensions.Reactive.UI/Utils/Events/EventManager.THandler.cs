using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.System;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Events;

internal interface IEventManager<THandler, TArgs>
	where TArgs : class
{
	/// <inheritdoc />
	void Add(THandler? handler);

	/// <inheritdoc />
	void Remove(THandler? handler);

	/// <inheritdoc />
	void Raise(TArgs args);

	/// <inheritdoc />
	void Raise(Func<TArgs> args);

	/// <inheritdoc />
	void Dispose();
}

//[Flags]
//internal enum EventConfig
//{
//	/// <summary>
//	/// Indicates that if multiple events are raised before the target thread can process it, only the last event is kept.
//	/// This is usually usefully for events that does not have info in their args (EventsArgs.Empty), e.g. ICommand.CanExecuteChanged
//	/// </summary>
//	IsCoalescable = 1,

//	/// <summary>
//	/// Indicates that the event can be subscribed only from UI thread.
//	/// Trying to add or remove an handler from a background thread will raise an exception.
//	/// </summary>
//	IsBackgroundThreadForbidden = 2,

//	/// <summary>
//	/// Indicates that event handlers might be registered from multiple thread.
//	/// </summary>
//	IsMultiThreaded = 4,
//}

internal class EventManager<THandler, TArgs> : IEventManager<THandler, TArgs>
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
		Func<DispatcherQueue?>? schedulersProvider = null)
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
		=> dispatcher switch
		{
			not null when _isCoalescable => new CoalescingDispatcherInvocationList<THandler, TArgs>(_owner, _raiseMethod, dispatcher),
			not null => new QueueingDispatcherInvocationList<THandler, TArgs>(_owner, _raiseMethod, dispatcher),
			null when _allowBgThread => new MultiThreadInvocationList<THandler, TArgs>(_owner, _raiseMethod),
			null => throw new InvalidOperationException("Cannot register an event handler from a background thread.")
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

//internal class MonoList<THandler, TArgs> : IEventManager<THandler, TArgs>
//	where TArgs : class
//{
//	private IInvocationList<THandler, TArgs> _list;

//	/// <inheritdoc />
//	public void Add(THandler? handler)
//	{
//		if (handler is not null)
//		{
//			GetList().Add(handler);
//		}
//	}

//	/// <inheritdoc />
//	public void Remove(THandler? handler)
//		=> _list.Remove(handler);

//	/// <inheritdoc />
//	public void Raise(TArgs args)
//		=> _list.Raise(args);

//	/// <inheritdoc />
//	public void Raise(Func<TArgs> args)
//		=> _list.Raise(args);

//	/// <inheritdoc />
//	public void Dispose()
//		=> _list.Dispose();

//	private IInvocationList<THandler, TArgs> GetList()
//	{
//		_list ??= DispatcherQueue.GetForCurrentThread() is {} dispatcher
//			? CreateInvocationList(dispatcher)
//			: 
//	}

//	private IInvocationList<THandler, TArgs> CreateInvocationList(DispatcherQueue? dispatcher)
//		=> (dispatcher, _isCoalescable) switch
//		{
//			(not null, true) => new CoalescingDispatcherInvocationList<THandler, TArgs>(_owner, _raiseMethod, dispatcher),
//			(not null, false) => new QueueingDispatcherInvocationList<THandler, TArgs>(_owner, _raiseMethod, dispatcher),
//			_ => new MultiThreadInvocationList<THandler, TArgs>(_owner, _raiseMethod)
//		};
//}
