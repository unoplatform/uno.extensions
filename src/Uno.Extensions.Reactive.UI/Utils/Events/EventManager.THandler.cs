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
		// TODO: Dispose All
	}
}

//internal interface IInvocationList<in THandler, in TArgs> : IEventManager<THandler, TArgs>
//{
//}

//internal class DispatcherInvocationList<THandler, TArgs> : IInvocationList<THandler, TArgs>
//	where TArgs : class
//{
//	private readonly object _owner;
//	private readonly Func<THandler, Action<object, TArgs>> _raise;
//	private readonly DispatcherQueue _dispatcher;

//	private List<THandler>? _handlers = new();
//	private bool _isEnumeratingHandlers;
//	private TArgs? _pending;

//	public DispatcherInvocationList(object owner, Func<THandler, Action<object, TArgs>> raiseMethod, DispatcherQueue dispatcher)
//	{
//		_owner = owner;
//		_raise = raiseMethod;
//		_dispatcher = dispatcher;
//	}

//	/// <inheritdoc />
//	public bool HasHandlers { get; private set; }

//	/// <inheritdoc />
//	public void Add(THandler handler)
//	{
//		if (GetHandlersForWrite() is { } handlers)
//		{
//			handlers.Add(handler);
//			HasHandlers = _handlers is not null; // Checks if disposed while adding item
//		}
//	}

//	/// <inheritdoc />
//	public void Remove(THandler handler)
//	{
//		if (GetHandlersForWrite() is { } handlers
//			&& handlers.Remove(handler))
//		{
//			HasHandlers = handlers.Count > 0 && _handlers is not null; // Checks if disposed while adding item
//		}
//	}

//	/// <inheritdoc />
//	public void Invoke(TArgs args)
//	{
//		if (HasHandlers)
//		{
//			_pending = args;
//			if (_dispatcher.HasThreadAccess)
//			{
//				RaiseCore();
//			}
//			else
//			{
//				_dispatcher.TryEnqueue(RaiseCore);
//			}
//		}
//	}

//	/// <inheritdoc />
//	public void Invoke(Func<TArgs> args)
//	{
//		if (HasHandlers)
//		{
//			_pending = args();
//			if (_dispatcher.HasThreadAccess)
//			{
//				RaiseCore();
//			}
//			else
//			{
//				_dispatcher.TryEnqueue(RaiseCore);
//			}
//		}
//	}

//	private void RaiseCore()
//	{
//		var args = Interlocked.Exchange(ref _pending, default);
//		var handlers = _handlers;
//		if (args is null || handlers is null)
//		{
//			return;
//		}

//		try
//		{
//			_isEnumeratingHandlers = true;
//			foreach (var handler in handlers)
//			{
//				_raise(handler!)(_owner, args);
//			}
//		}
//		finally
//		{
//			_isEnumeratingHandlers = false;
//		}
//	}

//	/// <inheritdoc />
//	public void Dispose()
//	{
//		_handlers = null;
//		HasHandlers = false;
//	}

//	private List<THandler>? GetHandlersForWrite()
//	{
//		if (_isEnumeratingHandlers && _handlers is {} handlers)
//		{
//			// Even if this method is expected to be invoked on the UI thread,
//			// we use interlocked here to avoir concurrency issue with Dispose
//			Interlocked.CompareExchange(ref _handlers, new List<THandler>(handlers), handlers);
//			_isEnumeratingHandlers = false;

//			return _handlers;
//		}
//		else
//		{
//			return _handlers;
//		}
//	}
//}



///// <summary>
///// An event manager which will capture the current thread when an handler is added, and raise the event on this thread
///// </summary>
//internal class EventManager<THandler, TArgs> : IEventManager<THandler, TArgs>
//{
//	private Dictionary<DispatcherQueue, List<THandler>> _uiHandlers = new();
//	private List<THandler> _bgHandlers = new();

//	private readonly CompositeDisposable _raiseSubscriptions = new CompositeDisposable();
//	private readonly SerialDisposable _serialRaiseSubscriptions = new SerialDisposable();

//	private readonly object _owner;
//	private readonly Func<THandler, Action<object, TArgs>> _raiseMethod;
//	private readonly bool _isCoalescable;
//	private readonly SynchronizationContext _default = new SynchronizationContext();
//	private ReaderWriterLockSlim _lock = new();

//	/// <summary>
//	/// Ctor
//	/// </summary>
//	/// <param name="owner">Owner of the event</param>
//	/// <param name="raiseMethod">Event handler invoke method</param>
//	/// <param name="isCoalescable">Determines if each call to <see cref="Raise"/> should abort any pending previous execution. SHOULD BE FALSE FOR PROPERTYCHANGED</param>
//	public EventManager(object owner, Func<THandler, Action<object, TArgs>> raiseMethod, bool isCoalescable = false)
//	{
//		_owner = owner;
//		_raiseMethod = raiseMethod;
//		_isCoalescable = isCoalescable;

//		_default = Schedulers.Default; // Capture it in order to always have the same instance
//	}

//	/// <inheritdoc/>
//	public bool HasHandlers => _uiHandlers.Any(kvp => kvp.Value.Count > 0);

//	/// <inheritdoc/>
//	public void Add(THandler handler)
//	{
//		var dispatcher = DispatcherQueue.GetForCurrentThread();
//		if (dispatcher is null)
//		{
//			lock (_bgHandlers)
//			{
//				_bgHandlers.Add(handler);
//			}
//		}
//		else
//		{
//			List<THandler> handlers;

//			_lock.EnterUpgradeableReadLock();
//			try
//			{
//				if (!_uiHandlers.TryGetValue(dispatcher, out handlers))
//				{
//					_lock.EnterWriteLock();
//					try
//					{
//						_uiHandlers[dispatcher] = handlers = new();
//					}
//					finally
//					{
//						_lock.ExitWriteLock();
//					}
//				}
//			}
//			finally
//			{
//				_lock.ExitUpgradeableReadLock();
//			}

//			NONE C'EST PAS BON : ON POURRAIT FAIRE UN REMOVE/ADD PENDANT UN RAISE
//			handlers.Add(handler);
//		}

//		var scheduler = SynchronizationContext.Current ?? _default;
//		while (true)
//		{
//			var capture = _uiHandlers;
//			if (capture == null)
//			{
//				return;
//			}

//			var handlersForCurrentThread = capture.GetValueOrDefault(scheduler) ?? ImmutableList<THandler>.Empty;
//			if (handlersForCurrentThread.Contains(handler))
//			{
//				return;
//			}

//			var updated = capture.SetItem(scheduler, handlersForCurrentThread.Add(handler));
//			if (Interlocked.CompareExchange(ref _uiHandlers, updated, capture) == capture)
//			{
//				return;
//			}
//		}
//	}

//	/// <inheritdoc/>
//	public void Remove(THandler handler)
//	{
//		var scheduler = Schedulers.FindCurrentDispatcher() ?? _default;
//		while (true)
//		{
//			var capture = _uiHandlers;
//			if (capture == null || !capture.TryGetValue(scheduler, out var handlersForCurrentThread))
//			{
//				return;
//			}

//			var updated = capture.SetItem(scheduler, handlersForCurrentThread.Remove(handler));
//			if (Interlocked.CompareExchange(ref _uiHandlers, updated, capture) == capture)
//			{
//				return;
//			}
//		}
//	}

//	/// <inheritdoc/>
//	public void RaiseArgs(TArgs args)
//	{
//		var handlers = _uiHandlers;
//		if (handlers == null || handlers.Count == 0)
//		{
//			return;
//		}

//		RaiseCore(handlers, args);
//	}

//	/// <inheritdoc/>
//	public void Raise(Func<TArgs> argsFactory)
//	{
//		var handlers = _uiHandlers;
//		if (handlers == null || handlers.Count == 0)
//		{
//			return;
//		}

//		RaiseCore(handlers, argsFactory());
//	}

//	private void RaiseCore(IImmutableDictionary<IScheduler, ImmutableList<THandler>> allHandlers, TArgs args)
//	{
//		var subscriptions = _isCoalescable
//			? (_serialRaiseSubscriptions.Disposable = new CompositeDisposable(allHandlers.Count)) as CompositeDisposable
//			: _raiseSubscriptions;

//		foreach (var kvp in allHandlers)
//		{
//			var scheduler = kvp.Key;
//			var handlersForScheduler = kvp.Value;

//			var disposable = new SingleAssignmentDisposable().DisposeWith(subscriptions);
//			disposable.Disposable = scheduler.Schedule(() =>
//			{
//				subscriptions.Remove(disposable);
//				foreach (var handler in handlersForScheduler)
//				{
//					_raiseMethod(handler)(_owner, args);
//				}
//			});
//		}
//	}

//	/// <inheritdoc/>
//	public void Dispose()
//	{
//		if (Interlocked.Exchange(ref _uiHandlers, null) == null)
//		{
//			return; // already disposed
//		}

//		_raiseSubscriptions.Dispose();
//		_serialRaiseSubscriptions.Dispose();
//	}
//}
