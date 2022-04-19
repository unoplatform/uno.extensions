#if NET6_0 && (__IOS__ || __ANDROID__)
#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.InteropServices.WindowsRuntime;

internal record struct EventRegistrationToken(Delegate Delegate)
{
}

internal class EventRegistrationTokenTable<THandler>
	where THandler : Delegate
{
	public EventRegistrationToken AddEventHandler(THandler handler)
	{
		InvocationList = (THandler)Delegate.Combine(InvocationList, handler);
		return default;
	}

	public void RemoveEventHandler(EventRegistrationToken token)
		=> RemoveEventHandler(token.Delegate as THandler);

	public void RemoveEventHandler(THandler handler)
		=> InvocationList = (THandler)Delegate.Remove(InvocationList, handler);

	public THandler InvocationList { get; private set; }
}
#endif
