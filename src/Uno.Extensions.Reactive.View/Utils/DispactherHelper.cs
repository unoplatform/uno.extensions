using System;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.System;

namespace Uno.Extensions.Reactive.View.Utils;

internal class DispatcherHelper
{
	public static DispatcherQueue GetDispatcher(DispatcherQueue? given = null)
		=> given
			?? DispatcherQueue.GetForCurrentThread()
			?? CoreApplication.MainView?.DispatcherQueue
			?? throw new InvalidOperationException("Failed to get dispatcher to use. Either explicitly provide the dispatcher to use, either make sure to invoke this on the UI thread.");
}
