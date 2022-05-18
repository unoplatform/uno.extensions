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
	public EventManager(object owner, bool isCoalescable = false)
		: base(owner, h => h.Invoke, isCoalescable)
	{
	}
}
