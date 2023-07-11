using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// This is a **VIRTUAL** <see cref="IMessage"/> that is used to represent the parent of a <see cref="DynamicFeed{T}"/>.
/// It should be used only by the <see cref="DynamicFeed{T}"/> and should never be propagated to subscriber in any way.
/// </summary>
internal sealed class DynamicParentMessage : IMessage
{
	public static DynamicParentMessage Initial { get; } = new(Entry.Empty, Entry.Empty, ChangeCollection.Empty);

	/// <inheritdoc />
	public IMessageEntry Previous { get; }

	/// <inheritdoc />
	public IMessageEntry Current { get; }

	/// <inheritdoc />
	public ChangeCollection Changes { get; }

	private DynamicParentMessage(IMessageEntry previous, IMessageEntry current)
		: this(previous, current, DetectChanges(previous, current))
	{
	}

	private DynamicParentMessage(IMessageEntry previous, IMessageEntry current, ChangeCollection changes)
	{
		Previous = previous;
		Current = current;
		Changes = changes;
	}

	public DynamicParentMessage With(IReadOnlyCollection<IMessage> parentMessages)
	{
		switch (parentMessages.Count)
		{
			case 0:
				if (object.ReferenceEquals(Current, Entry.Empty))
				{
					// No parent, but current was also built without any parent.
					return new(Current, Entry.Empty, ChangeCollection.Empty);
				}
				else
				{
					// No parent, but we had one, we need to detect changes.
					return new(Current, Entry.Empty);
				}

			case 1:
				var singleParent = parentMessages.First();
				if (singleParent.Previous == Current)
				{
					// The parent message is just an update of the previous one, we can keep all its values.
					return new(Current, singleParent.Current, singleParent.Changes);
				}
				else
				{
					// The relation between the parent message is and our current state is unknown, we need to detect changes.
					return new(Current, singleParent.Current);
				}

			default:
				var values = parentMessages
					.SelectMany(msg => msg.Current)
					.Where(kvp => kvp.Key != MessageAxis.Data) // We do not propagate the Data axis on "parent message".
					.GroupBy(kvp => kvp.Key)
					.ToDictionary(
						group => group.Key,
						group => group.Key.Aggregate(group.Select(v => v.Value)));
				var updated = new Entry(parentMessages, values);

				return new(Current, updated);
		}
	}

	private static ChangeCollection DetectChanges(IMessageEntry previous, IMessageEntry current)
	{
		var changes = new ChangeCollection();
		foreach (var kvp in current)
		{
			var axis = kvp.Key;
			if (axis.AreEquals(previous[kvp.Key], kvp.Value))
			{
				changes.Set(axis); // NTH: Add support for IChangeSet
			}
		}

		return changes;
	}

	private sealed record Entry(IReadOnlyCollection<IMessage> Parents, IReadOnlyDictionary<MessageAxis, MessageAxisValue> Values) : IMessageEntry
	{
		// We keep an undefined Data in order to avoid usage 
		public static Entry Empty { get; } = new(Array.Empty<IMessage>(), new Dictionary<MessageAxis, MessageAxisValue> { { MessageAxis.Data, new(Option<object>.Undefined()) } });

		/// <inheritdoc />
		Option<object> IMessageEntry.Data => throw new NotSupportedException("Not supported for dynamic messages.");

		/// <inheritdoc />
		Exception? IMessageEntry.Error => throw new NotSupportedException("Not supported for dynamic messages.");

		/// <inheritdoc />
		bool IMessageEntry. IsTransient => throw new NotSupportedException("Not supported for dynamic messages.");

		/// <inheritdoc />
		public MessageAxisValue this[MessageAxis axis]
			=> Values.TryGetValue(axis, out var value) ? value : MessageAxisValue.Unset;

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
		/// <inheritdoc />
		public IEnumerator<KeyValuePair<MessageAxis, MessageAxisValue>> GetEnumerator()
			=> Values.GetEnumerator();
	}
}
