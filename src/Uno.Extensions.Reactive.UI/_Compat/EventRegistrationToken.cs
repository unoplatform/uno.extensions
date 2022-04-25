#nullable disable // Matches the WinRT API
#if NET6_0_OR_GREATER && (__IOS__ || __ANDROID__)
#define IS_NET6_MOBILE
#endif
#if WINDOWS && WINUI
#define IS_WINDOWS_WINUI 
#endif

using System;
using System.Collections.Generic;
using System.Linq;

#if WINUI && (WINDOWS || IS_NET6_MOBILE)
namespace WinRT;
#else
namespace System.Runtime.InteropServices.WindowsRuntime;
#endif

#if IS_NET6_MOBILE
internal record struct EventRegistrationToken(long Value);
#endif

#if IS_NET6_MOBILE || IS_WINDOWS_WINUI
internal class EventRegistrationTokenTable<THandler>
	where THandler : Delegate
{
	private readonly List<long> _handlersTokenIds = new();
	private readonly List<WeakReference<THandler>> _handlers = new();

	private long _nextTokenId;

	public EventRegistrationToken AddEventHandler(THandler handler)
	{
		var token = new EventRegistrationToken(_nextTokenId++);
		var weakHandler = new WeakReference<THandler>(handler);

		_handlersTokenIds.Add(token.Value);
		_handlers.Add(weakHandler);

		return token;
	}

	public void RemoveEventHandler(EventRegistrationToken token)
	{
		var index = _handlersTokenIds.IndexOf(token.Value);
		if (index >=0)
		{
			_handlersTokenIds.RemoveAt(index);
			_handlers.RemoveAt(index);
		}
	}

	public void RemoveEventHandler(THandler handler)
	{
		var index = _handlers.FindIndex(weakHandler => weakHandler.TryGetTarget(out var h) && h == handler);
		if (index >= 0)
		{
			_handlersTokenIds.RemoveAt(index);
			_handlers.RemoveAt(index);
		}
	}

	public THandler InvocationList
	{
		get
		{
			var activeHandlers = new THandler[_handlers.Count];
			var activeHandlersCount = 0;
			for (var i = 0; i < _handlers.Count; )
			{
				if (_handlers[i].TryGetTarget(out var handler))
				{
					activeHandlers[activeHandlersCount++] = handler;
					i++;
				}
				else
				{
					_handlersTokenIds.RemoveAt(i);
					_handlers.RemoveAt(i);
				}
			}

			if (activeHandlersCount is 0)
			{
				return null;
			}

			Array.Resize(ref activeHandlers, activeHandlersCount);

			return (THandler)Delegate.Combine(activeHandlers);
		}
	}
}
#endif
