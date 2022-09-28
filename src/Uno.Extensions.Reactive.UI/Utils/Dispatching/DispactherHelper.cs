using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Dispatching;

internal class DispatcherHelper
{
	public delegate IDispatcherInternal? FindDispatcher();

	public static IDispatcherInternal GetDispatcher()
		=> GetDispatcher(null);

	public static IDispatcherInternal GetDispatcher(IDispatcherInternal? given)
		=> given
			?? GetForCurrentThread()
			?? throw new InvalidOperationException("Failed to get dispatcher to use. Either explicitly provide the dispatcher to use, either make sure to invoke this on the UI thread.");

	public static FindDispatcher GetForCurrentThread = DispatcherQueueProvider.GetForCurrentThread;
}
