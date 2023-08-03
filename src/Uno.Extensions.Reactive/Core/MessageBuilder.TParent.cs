using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A builder of <see cref="Message{T}"/> based on a parent message of <typeparamref name="TParent"/>.
/// </summary>
/// <typeparam name="TParent">Type of the value of the parent message.</typeparam>
/// <typeparam name="TResult">The type of the value of the message to build.</typeparam>
public class MessageBuilder<TParent, TResult> : IMessageEntry, IMessageEntry<TResult>, IMessageBuilder, IMessageBuilder<TResult>
{
	private readonly Dictionary<MessageAxis, MessageAxisUpdate> _updates;
	private bool _hasUpdates; // This allows us to easily determine if we have changes no matter if we removed axis axises flagged has IsTransient.
	private readonly IMessage? _parent;
	private readonly Message<TResult> _currentLocal; // This is the last message published by the manager. Unlike the 'Parent' is does not reflect the updates made on this builder.

	/// <summary>
	/// Creates a new message builder, including some changes (a.k.a. updates) that was previously made on the local message.
	/// </summary>
	/// <param name="parent">The last message received from the parent, if any.</param>
	/// <param name="local">The last message produced by the local Feed.</param>
	internal MessageBuilder(
		IMessage? parent,
		(IReadOnlyDictionary<MessageAxis, MessageAxisUpdate> updates, Message<TResult> value) local)
	{
		_parent = parent;
		_currentLocal = local.value;

		// We make sure to clear all transient axes when we update a message
		// Note: We remove only "local" values, parent values are still propagated, it's their responsibility to remove them.
		_updates = local.updates.ToDictionaryWhereKey(k => !k.IsTransient);
		// _hasUpdates = false => Removing only transient axes is not considered as a change!
	}

	/// <summary>
	/// Creates a new instance without any pending local changes
	/// </summary>
	/// <param name="parent">The last message received from the parent, if any.</param>
	/// <param name="local">The last message produced by the local Feed.</param>
	internal MessageBuilder(IMessage? parent, Message<TResult> local)
	{
		_parent = parent;
		_currentLocal = local;

		_updates = new();
		_hasUpdates = true; // When we drop the local changes, we should consider that we have changes.
	}

	/// <summary>
	/// The last message received from the parent, if any.
	/// </summary>
	/// <remarks>This is the "updated" parent defined on this builder!</remarks>
	internal Message<TParent>? Parent => _parent as Message<TParent>;

	/// <summary>
	/// The new set of updates that has been defined on this builder
	/// </summary>
	internal (IMessage? parent, bool hasUpdates, IReadOnlyDictionary<MessageAxis, MessageAxisUpdate> updates) GetResult()
		=> (_parent, _hasUpdates, _updates);

	#region IMessageEntry
	Option<object> IMessageEntry.Data => CurrentData;
	Option<TResult> IMessageEntry<TResult>.Data => CurrentData;
	Exception? IMessageEntry.Error => CurrentError;
	bool IMessageEntry.IsTransient => CurrentIsTransient;
	MessageAxisValue IMessageEntry.this[MessageAxis axis] => Get(axis).value;
	IEnumerator<KeyValuePair<MessageAxis, MessageAxisValue>> IEnumerable<KeyValuePair<MessageAxis, MessageAxisValue>>.GetEnumerator()
		=> throw new NotSupportedException("Axes enumeration is not supported on message builder.");
	IEnumerator IEnumerable.GetEnumerator()
		=> throw new NotSupportedException("Axes enumeration is not supported on message builder.");
	#endregion

	internal Option<TResult> CurrentData => MessageAxis.Data.FromMessageValue<TResult>(Get(MessageAxis.Data).value);
	internal Exception? CurrentError => MessageAxis.Error.FromMessageValue(Get(MessageAxis.Error).value);
	internal bool CurrentIsTransient => MessageAxis.Progress.FromMessageValue(Get(MessageAxis.Progress).value);

	/// <inheritdoc />
	(MessageAxisValue value, IChangeSet? changes) IMessageBuilder.Get(MessageAxis axis)
		=> Get(axis);
	internal (MessageAxisValue value, IChangeSet? changes) Get(MessageAxis axis)
	{
		var parentValue = _parent?.Current[axis] ?? MessageAxisValue.Unset;
		var localValue = _currentLocal.Current[axis];

		return _updates.TryGetValue(axis, out var updater)
			? updater.GetValue(parentValue, localValue)
			: (parentValue, default);
	}

	/// <inheritdoc />
	void IMessageBuilder.Set(MessageAxis axis, MessageAxisValue value, IChangeSet? changes)
		=> Set(axis, value, changes);

	internal MessageBuilder<TParent, TResult> Set(MessageAxis axis, MessageAxisValue value, IChangeSet? changes)
	{
		// Note: We are not validating the axis.AreEquals as changes are detected by the MessageManager itself.
		_updates[axis] = new MessageAxisUpdate(axis, value, changes);
		_hasUpdates = true;
		return this;
	}
}
