using System;
using System.Linq;
using Windows.ApplicationModel.Core;
#if !WINUI
using Windows.System;
#else
using Microsoft.UI.Dispatching;
#endif

namespace Uno.Extensions.Reactive.Utils;

internal class DispatcherHelper
{
	public delegate void TryEnqueueHandler(DispatcherQueue? given, DispatcherQueueHandler callback);

	public static DispatcherQueue GetDispatcher(DispatcherQueue? given = null)
		=> given
			?? DispatcherQueue.GetForCurrentThread()
#if !WINUI
			?? CoreApplication.MainView?.DispatcherQueue
#endif
			?? throw new InvalidOperationException("Failed to get dispatcher to use. Either explicitly provide the dispatcher to use, either make sure to invoke this on the UI thread.");

	public static TryEnqueueHandler TryEnqueue = (given, callback) => GetDispatcher(given).TryEnqueue(callback);
}
