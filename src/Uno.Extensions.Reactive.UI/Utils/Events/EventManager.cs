using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Events;

/// <summary>
/// An event manager which will capture the current thread when an handler is added, and raise the event on this thread
/// </summary>
internal class EventManager<TArgs> : EventManager<EventHandler<TArgs>, TArgs>
	where TArgs : class
{
	/// <summary>
	/// Ctor
	/// </summary>
	/// <param name="owner">Owner of the event.</param>
	/// <param name="isCoalescable">Determines if each call to Raise should abort any pending previous execution.</param>
	/// <param name="isBgThreadAllowed">Indicates if the manager allows registration of handler from background thread.</param>
	/// <param name="schedulersProvider">Specifies the provider of dispatcher.</param>
	public EventManager(object owner, bool isCoalescable = false, bool isBgThreadAllowed = true, Func<DispatcherQueue?>? schedulersProvider = null)
		: base(owner, h => h.Invoke, isCoalescable, isBgThreadAllowed, schedulersProvider)
	{
	}
}
