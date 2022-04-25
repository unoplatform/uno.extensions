#if NET5_0_OR_GREATER
#nullable disable // Matches the WinRT API


#if __ANDROID__ || __IOS__
// Both types are missing on NET6_MOBILE, for UWP and WinUI
// Note: They are added only to share code, they are actually not exposed publicly

#define NEEDS_EVT_TOKEN
#define NEEDS_EVT_TOKEN_TABLE

#elif WINUI
// Types might be in old interop.WinRun namespace, but we want to match the WinUI API and use the WinRT namespace.

#if !WINDOWS // net6-win is the only platform that defines the WinRT.EvtRegToken (but not the table)
#define NEEDS_EVT_TOKEN
#endif
#define NEEDS_EVT_TOKEN_TABLE // Type might be in old interop.WinRun namespace, but we want to match the WinUI API and use the WinRT namespace

#endif

using System;
using System.Collections.Generic;
using System.Linq;

#if WINUI 
namespace WinRT;
#else
namespace System.Runtime.InteropServices.WindowsRuntime;
#endif

#if NEEDS_EVT_TOKEN
internal record struct EventRegistrationToken(long Value);
#endif

#if NEEDS_EVT_TOKEN_TABLE
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
#endif
