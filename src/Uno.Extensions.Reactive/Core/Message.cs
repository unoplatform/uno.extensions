using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A message of an <see cref="IFeed{T}"/>.
/// </summary>
/// <typeparam name="T">The value type of the message.</typeparam>
public sealed class Message<T> : IMessage
{
	/// <summary>
	/// The initial message of <see cref="IFeed{T}"/>.
	/// </summary>
	public static Message<T> Initial { get; } = new(MessageEntry<T>.Empty, MessageEntry<T>.Empty, ChangeCollection.Empty);

	internal Message(MessageEntry<T> previous, MessageEntry<T> current, ChangeCollection changes)
	{
		Previous = previous;
		Current = current;
		Changes = changes;
	}

	/// <summary>
	/// The previous entry.
	/// </summary>
	public MessageEntry<T> Previous { get; }
	IMessageEntry IMessage.Previous => Previous;

	/// <summary>
	/// The current entry.
	/// </summary>
	public MessageEntry<T> Current { get; }
	IMessageEntry IMessage.Current => Current;

	/// <summary>
	/// The axes that has been modified in <see cref="Current"/> compared to <see cref="Previous"/>.
	/// </summary>
	public ChangeCollection Changes { get; }

	/// <summary>
	/// Begins creation of a new message based on this current message.
	/// </summary>
	/// <returns>A builder to configure the updated message to build.</returns>
	public MessageBuilder<T> With()
		=> new(Current);

	internal Message<T> OverrideBy(Message<T> newMessage) 
	{
		if (Current == newMessage.Previous)
		{
			return newMessage;
		}

		var oldValues = Current.Values;
		var newValues = newMessage.Current.Values;

		var changes = new ChangeCollection();
		foreach (var kvp in newValues)
		{
			var axis = kvp.Key;
			var currentValue = Current[axis];
			var updatedValue = kvp.Value;
			if (axis.AreEquals(currentValue, updatedValue))
			{
				continue;
			}
			if (newMessage.Changes.Contains(axis, out var changeSet)
				&& changeSet is not null
				&& axis.AreEquals(currentValue, newMessage.Previous[axis]))
			{
				// If we have a changeSet and it applies tu update from the currentValue, we propagate it
				changes.Set(axis, changeSet);
			}
			else
			{
				changes.Set(axis);
			}
		}

		foreach (var removedAxis in oldValues.Keys.Except(newValues.Keys))
		{
			changes.Set(removedAxis);
		}

		return new Message<T>(Current, newMessage.Current, changes);
	}
}
