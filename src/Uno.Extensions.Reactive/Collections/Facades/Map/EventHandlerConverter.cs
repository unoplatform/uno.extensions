using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Uno.Events;

/// <summary>
/// A helper to convert the type of an event handler and manage the subscriptions.
/// </summary>
/// <typeparam name="TFromHandler">The source handler</typeparam>
/// <typeparam name="TToHandler">The target handler</typeparam>
internal class EventHandlerConverter<TFromHandler, TToHandler>
	where TFromHandler : notnull
{
	private ImmutableDictionary<TFromHandler, TToHandler> _handlers = ImmutableDictionary<TFromHandler, TToHandler>.Empty;

	private readonly Func<TFromHandler, TToHandler> _convert;
	private readonly Action<TToHandler> _add;
	private readonly Action<TToHandler> _remove;

	public EventHandlerConverter(
		Func<TFromHandler, TToHandler> convert,
		Action<TToHandler> add,
		Action<TToHandler> remove)
	{
		_convert = convert;
		_add = add;
		_remove = remove;
	}

	/// <summary>
	/// Subscribe to the inner event
	/// </summary>
	public void Add(TFromHandler from)
	{
		var to = _convert(from);
		ImmutableInterlocked.Update(ref _handlers, h => h.Add(from, to));
		_add(to);
	}

	/// <summary>
	/// Unsubscribe from the inner event
	/// </summary>
	public void Remove(TFromHandler from)
	{
		if (ImmutableInterlocked.TryRemove(ref _handlers, from, out var to))
		{
			_remove(to);
		}
	}
}
