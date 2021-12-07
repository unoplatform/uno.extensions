using System;
using System.Linq;
using Windows.ApplicationModel.Core;

namespace Uno.Extensions.Reactive.Dispatching;

internal class DispatcherHelper
{
	public delegate DispatcherQueue? FindDispatcher();

	public static DispatcherQueue GetDispatcher(DispatcherQueue? given = null)
		=> given
			?? GetForCurrentThread()
#if !WINUI
			?? CoreApplication.MainView?.DispatcherQueue
#endif
			?? throw new InvalidOperationException("Failed to get dispatcher to use. Either explicitly provide the dispatcher to use, either make sure to invoke this on the UI thread.");

	public static FindDispatcher GetForCurrentThread = DispatcherQueue.GetForCurrentThread;
}
