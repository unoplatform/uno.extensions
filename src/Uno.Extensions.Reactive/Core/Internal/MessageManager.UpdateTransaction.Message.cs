using System;
using System.Collections.Generic;
using System.ComponentModel;
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
				=> new(_transaction, new MessageManager<TParent, TResult>.CurrentMessage(_transaction._owner).With());

			public MessageBuilder With(Message<TParent>? updatedParent)
				=> new(_transaction, new MessageManager<TParent, TResult>.CurrentMessage(_transaction._owner).With(updatedParent));

			// Internal dedicated to the DynamicFeed. Should not be used outside of it.
			public MessageBuilder With(IMessage? updatedParent)
				=> new(_transaction, new MessageManager<TParent, TResult>.CurrentMessage(_transaction._owner).With(updatedParent));
		}

		/// <summary>
		/// A <see cref="MessageBuilder{TParent, TResult}"/> dedicated for <see cref="UpdateTransaction"/>
		/// that allows to set transient value only for the lifetime of the transaction.
		/// </summary>
		internal readonly struct MessageBuilder : IMessageBuilder<TResult> // TODO: This could now inherit from MessageBuilder<TParent, TResult> (we can then remove the access to the _currentLocal)
		{
			private readonly UpdateTransaction _transaction;
			private readonly Dictionary<MessageAxis, MessageAxisUpdate> _transientUpdates;

			public MessageBuilder(
				UpdateTransaction transaction,
				MessageBuilder<TParent, TResult> inner)
			{
				_transaction = transaction;
				_transientUpdates = transaction._transientUpdates;
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

			internal (MessageAxisValue value, IChangeSet? changes) Get(MessageAxis axis)
			{
				if (_transientUpdates.TryGetValue(axis, out var updater))
				{
					var parentValue = Inner.Parent?.Current[axis] ?? MessageAxisValue.Unset;
					var localValue = _transaction.Local.Current[axis];

					return updater.GetValue(parentValue, localValue);
				}
				else
				{
					return Inner.Get(axis);
				}
			}

			/// <inheritdoc />
			void IMessageBuilder.Set(MessageAxis axis, MessageAxisValue value, IChangeSet? changes)
				=> Inner.Set(axis, value, changes);

			internal void Set(MessageAxis axis, MessageAxisValue value, IChangeSet? changes = null)
				=> Inner.Set(axis, value, changes);
		}
	}
}
