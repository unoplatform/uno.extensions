using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Events;

/// <summary>
/// A manager of event handler responsible to manage handler list, and raise the event properly.
/// </summary>
/// <typeparam name="THandler"></typeparam>
/// <typeparam name="TArgs"></typeparam>
internal interface IInvocationList<in THandler, in TArgs> : IDisposable
{
	/// <summary>
	/// Gets a bool which indicates if the they are any handlers currently subscribed
	/// </summary>
	bool HasHandlers { get; }

	/// <summary>
	/// Adds an handler to the event.
	/// </summary>
	void Add(THandler handler);

	/// <summary>
	/// Removes an handle from the event.
	/// </summary>
	void Remove(THandler handler);

	/// <summary>
	/// Raise the event to all handlers
	/// </summary>
	void Invoke(TArgs args);

	/// <summary>
	/// Raise the event to all handlers
	/// </summary>
	void Invoke(Func<TArgs> args);
}
