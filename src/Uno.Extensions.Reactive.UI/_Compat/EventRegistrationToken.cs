#if !USE_EVENT_TOKEN
#nullable disable // Matches the UWP API

using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.UI._Compat;

// The EventRegsitrationToken is use only in WinRT for native events.
// On all other platforms, in order to share teh code, we have our own implementation where the EventRegistrationToken is nothing.
// Note: Even if on some platforms the EventRegistrationToken and the EventRegistrationTokenTable appears as present,
//		 and compilation of the Reactive.UI package succeed, the resolution of the type at runtime might fail (TypeLoadException).
//		 So to avoid any crash at runtime, we prefer to always use our how version (except on UWP which needs it)!

internal record struct EventRegistrationToken;

internal class EventRegistrationTokenTable<THandler>
	where THandler : Delegate
{
	private readonly List<THandler> _handlers = new();

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
		_handlers.Add(handler);
		_isInvocationListValid = false;

		return default;
	}

	public void RemoveEventHandler(THandler handler)
	{
		var index = _handlers.IndexOf(handler);
		if (index >= 0)
		{
			_handlers.RemoveAt(index);
			_invocationList = null; // prevent leak
			_isInvocationListValid = false;
		}
	}
}
#endif
