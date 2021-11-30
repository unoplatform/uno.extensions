using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

internal sealed partial class MessageManager<TParent, TResult>
{
	public struct CurrentMessage
	{
		private readonly MessageManager<TParent, TResult> _owner;

		internal CurrentMessage(MessageManager<TParent, TResult> owner)
		{
			_owner = owner;
		}

		public Message<TParent>? Parent => _owner._parent;

		public Message<TResult> Local => _owner.Current;

		public MessageBuilder<TParent, TResult> With() 
			=> new(Parent, (_owner._local.defined, _owner._local.result));

		public MessageBuilder<TParent, TResult> With(Message<TParent>? updatedParent)
			=> new(updatedParent ?? Parent, (_owner._local.defined, _owner._local.result));
	}
}
