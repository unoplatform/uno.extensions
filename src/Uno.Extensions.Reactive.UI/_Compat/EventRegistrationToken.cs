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
	private readonly List<THandler> _handlers = new();

	private long _nextTokenId;

	private bool _isInvocationListValid = true;
	private THandler _invocationList;

	public THandler InvocationList
	{
		get
		{
			if (!_isInvocationListValid)
			{
				_invocationList = _handlers.Count switch
				{
					0 => null,
					1 => _handlers[0],
					_ => (THandler)Delegate.Combine(_handlers.ToArray<Delegate>())
				};
				_isInvocationListValid = true;
			}

			return _invocationList;
		}
	}

	public EventRegistrationToken AddEventHandler(THandler handler)
	{
		var token = new EventRegistrationToken(_nextTokenId++);

		_handlersTokenIds.Add(token.Value);
		_handlers.Add(handler);
		_isInvocationListValid = false;

		return token;
	}

	public void RemoveEventHandler(EventRegistrationToken token)
	{
		var index = _handlersTokenIds.IndexOf(token.Value);
		if (index >= 0)
		{
			_handlersTokenIds.RemoveAt(index);
			_handlers.RemoveAt(index);
			_invocationList = null; // prevent leak
			_isInvocationListValid = false;
		}
	}

	public void RemoveEventHandler(THandler handler)
	{
		var index = _handlers.IndexOf(handler);
		if (index >= 0)
		{
			_handlersTokenIds.RemoveAt(index);
			_handlers.RemoveAt(index);
			_invocationList = null; // prevent leak
			_isInvocationListValid = false;
		}
	}
}
#endif
#endif
