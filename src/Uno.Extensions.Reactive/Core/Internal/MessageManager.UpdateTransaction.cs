using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal partial class MessageManager<TParent, TResult>
{
	public class UpdateTransaction : IDisposable
	{
		private readonly MessageManager<TParent, TResult> _owner;
		private readonly Dictionary<MessageAxis, MessageAxisUpdate> _transientUpdates;
		private readonly CancellationToken _ct;
		private readonly CancellationTokenRegistration _ctSubscription;

		private int _state = State.Active;

		private static class State
		{
			public const int Active = 0;
			public const int Committed = 1;
			public const int Disposed = 255;
		}

		internal IReadOnlyDictionary<MessageAxis, MessageAxisUpdate> TransientUpdates => _transientUpdates;

		public Message<TParent>? Parent => _owner._parent;

		public Message<TResult> Local => _owner.Current;

		public UpdateTransaction(MessageManager<TParent, TResult> owner, CancellationToken ct)
			: this(owner, new(), ct)
		{
		}

		public UpdateTransaction(MessageManager<TParent, TResult> owner, Dictionary<MessageAxis, MessageAxisUpdate> existingUpdates, CancellationToken ct)
		{
			_owner = owner;
			_transientUpdates = existingUpdates;
			_ct = ct;
			_ctSubscription = ct.Register(Dispose);
		}

		public void TransientSet(MessageAxis axis, object value)
		{
			if (_state != State.Active)
			{
				return;
			}

			_owner.Update(
				m =>
				{
					// We alter the _transientUpdates in the 'updater' delegate so we are thread safe thanks to the _owner._gate
					_transientUpdates[axis] = new MessageAxisUpdate(axis, new MessageAxisValue(value));

					return m.With();
				}, 
				_ct);
		}

		public void TransientSetWithFinalParentUpdate(MessageAxis axis, object value, Message<TParent>? parentMsg)
		{
			if (_state != State.Active)
			{
				return;
			}

			_owner.Update(
				m =>
				{
					// We alter the _transientUpdates in the 'updater' delegate so we are thread safe thanks to the _owner._gate
					_transientUpdates[axis] = new MessageAxisUpdate(axis, new MessageAxisValue(value));

					return m.With(parentMsg);
				},
				_ct);
		}

		public void Commit(Func<CurrentMessage, MessageBuilder<TParent, TResult>> resultUpdater)
		{
			if (Interlocked.CompareExchange(ref _state, State.Committed, State.Active) == State.Active)
			{
				_owner.EndUpdate(this, resultUpdater);
			}

			Dispose();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_ctSubscription.Dispose();
			if (Interlocked.Exchange(ref _state, State.Disposed) == State.Active)
			{
				_owner.EndUpdate(this);
			}

			GC.SuppressFinalize(this);
		}

		~UpdateTransaction()
		{
			Dispose();
		}
	}
}
