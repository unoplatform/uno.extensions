using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

internal partial class MessageManager<TParent, TResult>
{
	partial class UpdateTransaction
	{
		public readonly struct CurrentMessage
		{
			private readonly UpdateTransaction _transaction;

			internal CurrentMessage(UpdateTransaction transaction)
			{
				_transaction = transaction;
			}

			public Message<TParent>? Parent => _transaction.Parent;

			public Message<TResult> Local => _transaction.Local;

			public MessageBuilder With()
				=> new(_transaction._transientUpdates, new MessageManager<TParent, TResult>.CurrentMessage(_transaction._owner).With());

			public MessageBuilder With(Message<TParent>? updatedParent)
				=> new(_transaction._transientUpdates, new MessageManager<TParent, TResult>.CurrentMessage(_transaction._owner).With(updatedParent));
		}

		/// <summary>
		/// A <see cref="MessageBuilder{TParent, TResult}"/> dedicated for <see cref="UpdateTransaction"/>
		/// that allows to set transient value only for the lifetime of the transaction.
		/// </summary>
		internal readonly struct MessageBuilder : IMessageBuilder<TResult>
		{
			private readonly Dictionary<MessageAxis, MessageAxisUpdate> _transientUpdates;

			public MessageBuilder(
				Dictionary<MessageAxis, MessageAxisUpdate> transientUpdates,
				MessageBuilder<TParent, TResult> inner)
			{
				_transientUpdates = transientUpdates;
				Inner = inner;
			}

			/// <summary>
			/// The wrapped message builder
			/// </summary>
			internal MessageBuilder<TParent, TResult> Inner { get; }

			/// <summary>
			/// Temporarily sets the raw value of an axis for the lifetime of the current transaction.
			/// Value will be automatically roll-backed at the end of the transaction.
			/// </summary>
			/// <param name="axis">The axis to set.</param>
			/// <param name="value">The raw value of the axis.</param>
			/// <remarks>
			/// This gives access to raw <see cref="MessageAxisValue"/> for extensibility but it should not be used directly.
			/// Prefer to use dedicated extensions methods to access to values.
			/// </remarks>
			public MessageBuilder SetTransient(MessageAxis axis, MessageAxisValue value)
			{
				_transientUpdates[axis] = new MessageAxisUpdate(axis, value);
				return this;
			}

			/// <inheritdoc />
			(MessageAxisValue value, IChangeSet? changes) IMessageBuilder.Get(MessageAxis axis)
				=> Get(axis);

			/// <inheritdoc />
			public void Set(MessageAxis axis, MessageAxisValue value, IChangeSet? changes = null)
				=> Inner.Set(axis, value, changes);

			internal (MessageAxisValue value, IChangeSet? changes) Get(MessageAxis axis)
			{
				var parentValue = Inner.Parent?.Current[axis] ?? MessageAxisValue.Unset;
				var localValue = Inner.Local.Current[axis];
				if (_transientUpdates.TryGetValue(axis, out var updater)
					|| Inner.GetResult().updates.TryGetValue(axis, out updater))
				{
					return updater.GetValue(parentValue, localValue);
				}
				else
				{
					return (parentValue, null);
				}
			}

			public MessageBuilder Apply(Action<MessageBuilder<TParent, TResult>>? configure)
			{
				configure?.Invoke(Inner);
				return this;
			}
		}
	}
}
