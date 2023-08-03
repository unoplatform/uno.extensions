using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

internal partial class MessageManager<TParent, TResult>
{
	// This structure is only to allow the same syntax as for the Message<T>.With(), e.g. updated = current.With().Data(myData);
	// Internally we are able to change the parent and clear all the defined values directly on the builder.
	public struct CurrentMessage
	{
		private readonly MessageManager<TParent, TResult> _owner;

		internal CurrentMessage(MessageManager<TParent, TResult> owner)
		{
			_owner = owner;
		}

		public Message<TParent>? Parent => _owner._parent as Message<TParent>;

		public Message<TResult> Local => _owner._local.result;

		internal MessageBuilder<TParent, TResult> WithParentOnly(Message<TParent>? updatedParent)
			=> new(updatedParent ?? Parent, _owner._local.result);

		public MessageBuilder<TParent, TResult> With() 
			=> new(Parent, (_owner._local.defined, _owner._local.result));

		public MessageBuilder<TParent, TResult> With(Message<TParent>? updatedParent)
			=> new(updatedParent ?? Parent, (_owner._local.defined, _owner._local.result));

		// Internal dedicated to the DynamicFeed. Should not be used outside of it.
		internal MessageBuilder<TParent, TResult> With(IMessage? updatedParent)
			=> new(updatedParent ?? Parent, (_owner._local.defined, _owner._local.result));
	}
}
