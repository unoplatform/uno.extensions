using System;
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
public sealed class MessageBuilder<TParent, TResult> : IMessageEntry, IMessageBuilder, IMessageBuilder<TResult>
{
	private readonly Dictionary<MessageAxis, MessageAxisUpdate> _updates;
	private bool _hasUpdates; // This allows us to easily determine if we have changes no matter if we removed axis axises flagged has IsTransient.

	/// <summary>
	/// Creates a new message builder, including some changes (a.k.a. updates) that was previously made on the local message.
	/// </summary>
	/// <param name="parent">The last message received from the parent, if any.</param>
	/// <param name="local">The last message produced by the local Feed.</param>
	internal MessageBuilder(
		Message<TParent>? parent,
		(IReadOnlyDictionary<MessageAxis, MessageAxisUpdate> updates, Message<TResult> value) local)
	{
		Parent = parent;
		Local = local.value;

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
	internal MessageBuilder(Message<TParent>? parent, Message<TResult> local)
	{
		Parent = parent;
		Local = local;

		_updates = new();
		_hasUpdates = true; // When we drop the local changes, we should consider that we have changes.
	}

	/// <summary>
	/// The last message received from the parent, if any.
	/// </summary>
	internal Message<TParent>? Parent { get; }

	/// <summary>
	/// The last message produced by the local Feed.
	/// </summary>
	internal Message<TResult> Local { get; }

	/// <summary>
	/// The new set of updates that has been defined on this builder
	/// </summary>
	internal (Message<TParent>? parent, bool hasUpdates, IReadOnlyDictionary<MessageAxis, MessageAxisUpdate> updates) GetResult()
		=> (Parent, _hasUpdates, _updates);

	Option<object> IMessageEntry.Data => CurrentData;
	Exception? IMessageEntry.Error => CurrentError;
	bool IMessageEntry.IsTransient => CurrentIsTransient;
	MessageAxisValue IMessageEntry.this[MessageAxis axis] => Get(axis).value;

	internal Option<TResult> CurrentData => MessageAxis.Data.FromMessageValue<TResult>(Get(MessageAxis.Data).value);
	internal Exception? CurrentError => MessageAxis.Error.FromMessageValue(Get(MessageAxis.Error).value);
	internal bool CurrentIsTransient => MessageAxis.Progress.FromMessageValue(Get(MessageAxis.Progress).value);

	/// <inheritdoc />
	(MessageAxisValue value, IChangeSet? changes) IMessageBuilder.Get(MessageAxis axis)
		=> Get(axis);
	internal (MessageAxisValue value, IChangeSet? changes) Get(MessageAxis axis)
	{
		var parentValue = Parent?.Current[axis] ?? MessageAxisValue.Unset;
		var localValue = Local.Current[axis];

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
