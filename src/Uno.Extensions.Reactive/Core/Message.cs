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
	public static Message<T> Initial { get; } = new(MessageEntry<T>.Empty, MessageEntry<T>.Empty, Array.Empty<MessageAxis>());

	internal Message(MessageEntry<T> previous, MessageEntry<T> current, IReadOnlyCollection<MessageAxis> changes)
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
	public IReadOnlyCollection<MessageAxis> Changes { get; }

	//public ChangesCollection Changes { get; }

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

		var modified = newValues
			.Where(kvp => !kvp.Key.AreEquals(Current[kvp.Key], kvp.Value))
			.Select(kvp => kvp.Key);
		var removed = oldValues.Keys.Except(newValues.Keys);

		return new Message<T>(Current, newMessage.Current, modified.Concat(removed).ToList());
	}
}


//public class ChangesCollection : IReadOnlyCollection<MessageAxis>
//{
//	public bool Contains(MessageAxis axis)
//	{

//	}

//	public bool Contains(MessageAxis axis, out IMessageAxisChange? change)
//	{

//	}
//}

public interface IMessageAxisChange
{
}
