using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal partial class MessageManager<TParent, TResult>
{
	public sealed partial class UpdateTransaction : IDisposable
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

		internal UpdateTransaction(MessageManager<TParent, TResult> owner, CancellationToken ct)
			: this(owner, new(), ct)
		{
		}

		internal UpdateTransaction(MessageManager<TParent, TResult> owner, Dictionary<MessageAxis, MessageAxisUpdate> existingUpdates, CancellationToken ct)
		{
			_owner = owner;
			_transientUpdates = existingUpdates;
			_ct = ct;
			_ctSubscription = ct.Register(Dispose);
		}

		public void Update(Func<CurrentMessage, MessageBuilder> updater)
			=> Update((cm, u) => u(cm), updater);

		public void Update<TState>(Func<CurrentMessage, TState, MessageBuilder> updater, TState state)
		{
			if (_state != State.Active)
			{
				return;
			}

			// Note: We alter the '_transientUpdates' in the 'updater' delegate so we are thread safe thanks to the _owner._gate
			//		 The '_transientUpdates' is made accessible through the SetTransient method of the MessageBuilder dedicated to transaction.
			_owner.Update(
				(m, @params) => @params.updater(new CurrentMessage(@params.that), @params.state).Inner,
				(that: this, updater, state),
				_ct);
		}

		/// <summary>
		/// Commits the transaction, cf. remarks for perf considerations.
		/// </summary>
		/// <remarks>
		/// For performance consideration,
		/// especially if you are using transient values (which is the purpose of this UpdateTransaction!),
		/// avoid to do something like:
		///		```csharp
		///		transaction.Update(msg => msg.With().Data(new object()));
		///		transaction.Commit();
		///		```
		/// but prefer to use the overloads of `Commit` that accepts an updater, e.g.:
		///		```csharp
		///		transaction.Commit(msg => msg.With().Data(new object()));
		///		```
		/// This will ensure to push only one message that removes transient axis values among the final update you are doing.
		/// </remarks>
		public void Commit()
		{
			if (Interlocked.CompareExchange(ref _state, State.Committed, State.Active) == State.Active)
			{
				_owner.EndUpdate(this);
			}

			Dispose();
		}

		/// <summary>
		/// Apply the final update of the message and commits the transaction.
		/// </summary>
		/// <param name="resultUpdater">The updater to build the final message produced by the operation wrapped by this transaction.</param>
		public void Commit(Updater resultUpdater)
			=> Commit(_stateLessUpdater, resultUpdater);

		/// <summary>
		/// Apply the final update of the message and commits the transaction.
		/// </summary>
		/// <typeparam name="TState">Type of the state used by the <paramref name="resultUpdater"/>.</typeparam>
		/// <param name="resultUpdater">The updater to build the final message produced by the operation wrapped by this transaction.</param>
		/// <param name="state">The argument to provide to the <paramref name="resultUpdater"/>.</param>
		public void Commit<TState>(Updater<TState> resultUpdater, TState state)
		{
			if (Interlocked.CompareExchange(ref _state, State.Committed, State.Active) == State.Active)
			{
				_owner.EndUpdate(this, resultUpdater, state);
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
