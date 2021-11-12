using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive;

public sealed class Message<T> : IMessage
{
	public static Message<T> Initial { get; } = new(MessageEntry<T>.Empty, MessageEntry<T>.Empty, Array.Empty<MessageAxis>());

	internal Message(MessageEntry<T> previous, MessageEntry<T> current, IReadOnlyCollection<MessageAxis> changes)
	{
		Previous = previous;
		Current = current;
		Changes = changes;
	}

	IMessageEntry IMessage.Previous => Previous;
	public MessageEntry<T> Previous { get; }

	IMessageEntry IMessage.Current => Current;
	public MessageEntry<T> Current { get; }

	public IReadOnlyCollection<MessageAxis> Changes { get; }

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

		var modified = newValues
			.Where(kvp => !kvp.Key.AreEquals(Current[kvp.Key], kvp.Value))
			.Select(kvp => kvp.Key);
		var removed = oldValues.Keys.Except(newValues.Keys);

		return new Message<T>(Current, newMessage.Current, modified.Concat(removed).ToList());
	}
}
