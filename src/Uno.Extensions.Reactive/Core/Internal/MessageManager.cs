using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Utils;
using _ChangeSet = System.Collections.Generic.IReadOnlyDictionary<Uno.Extensions.Reactive.MessageAxis, Uno.Extensions.Reactive.Core.MessageAxisUpdate>;

namespace Uno.Extensions.Reactive.Core;

internal sealed partial class MessageManager<TParent, TResult>
{
	private readonly object _gate = new();
	private readonly Action<Message<TResult>>? _send;

	private Message<TParent>? _parent;
	private UpdateTransaction? _pendingUpdate;

	public Message<TResult> Current => _local.result;
	// Locally, we only store a set of delegates that are upgrading the parent value into a local value.
	private (_ChangeSet defined, _ChangeSet applied, Message<TResult> result) _local;

	private bool _isFirstUpdate = true;

	public MessageManager(Action<Message<TResult>>? send = null)
	{
		_send = send;

		var initialMessage = Message<TResult>.Initial;
		var initialUpdates = new Dictionary<MessageAxis, MessageAxisUpdate>
		{
			{ MessageAxis.Data, new(MessageAxis.Data, new(Option<object>.Undefined())) { IsOverride = true } }
		};
		_local = (initialUpdates, initialUpdates, initialMessage);
	}

	public bool Update(Func<CurrentMessage, MessageBuilder<TParent, TResult>> updater, CancellationToken ct = default)
	{
		// Even if this method is sync, we force the caller to provide a ct to make sure that we don't send an update if cancelled
		if (ct.IsCancellationRequested)
		{
			return false;
		}

		lock (_gate)
		{
			var (parent, locallyDefinedChangeSet) = updater(new CurrentMessage(this)).GetResult();

			if (ct.IsCancellationRequested)
			{
				return false;
			}

			// If we have any pending update transaction, we make sure to append it's change set to the locally defined
			var changeSetToApply = _pendingUpdate?.TransientUpdates is { Count: > 0 } transientUpdates
				? locallyDefinedChangeSet.ToDictionary().SetItems(transientUpdates)
				: locallyDefinedChangeSet;

			// Finally apply the updates in order to get the new Local
			// Note: We append the _local.applied.Keys as if a transaction was removed, it's possible that some changes was removed
			var possiblyChangedAxes = changeSetToApply.Keys.Concat(_local.applied.Keys);
			if (parent is not null && parent != _parent) // Note: parent should not be null if updated !!!
			{
				possiblyChangedAxes = possiblyChangedAxes.Concat(parent.Changes);
			}

			var parentEntry = parent?.Current ?? MessageEntry<TParent>.Empty;
			var localEntry = _local.result.Current;
			var values = localEntry.Values.ToDictionary();
			var changes = new List<MessageAxis>();
			foreach (var axis in possiblyChangedAxes.Distinct())
			{
				var parentValue = parentEntry[axis];
				var currentValue = localEntry[axis];

				// Note: If we don't have any "change" to apply to the given axis,
				//		 it means that either that "change" has been removed (for instance a transient from an update transaction),
				//		 either the change is coming from the parent.
				//		 In all case we just need to propagate the value from the parent.
				var updatedValue = changeSetToApply.TryGetValue(axis, out var update)
					? update.GetValue(parentValue, currentValue)
					: parentValue;

				if (updatedValue == MessageAxisValue.Unset)
				{
					values.Remove(axis);
				}
				else
				{
					values[axis] = updatedValue;
				}

				if (!axis.AreEquals(currentValue, updatedValue))
				{
					changes.Add(axis);
				}
			}

			_parent = parent;

			if (!_isFirstUpdate && changes is { Count: 0 })
			{
				return false; // Well even if some changes was made on the Parent and/or on Local, the resulting values are the same.
			}

			_isFirstUpdate = false;
			_local = (locallyDefinedChangeSet, changeSetToApply, new Message<TResult>(Current.Current, new MessageEntry<TResult>(values), changes)); 
			_send?.Invoke(Current);
			return true;
		}
	}

	private void Update()
		=> Update(m => m.With(), CancellationToken.None);

	public UpdateTransaction BeginUpdate(CancellationToken ct)
	{
		lock (_gate)
		{
			var previousTransaction = _pendingUpdate;
			var transaction = new UpdateTransaction(this, ct);

			_pendingUpdate = transaction;
			if (previousTransaction is not null)
			{
				previousTransaction.Dispose();
				if (previousTransaction.TransientUpdates.Any())
				{
					Update(); // Make sure to clear the transient updates
				}
			}

			return transaction; 
		}
	}

	public UpdateTransaction BeginUpdate(CancellationToken ct, params MessageAxis[] preservePendingAxes)
	{
		lock (_gate)
		{
			var previousTransaction = _pendingUpdate;
			var existingTransientUpdates = previousTransaction
					?.TransientUpdates
					.Values
					.Where(u => preservePendingAxes.Contains(u.Axis))
					.ToDictionary(u => u.Axis)
				?? new();
			var transaction = new UpdateTransaction(this, existingTransientUpdates, ct);

			_pendingUpdate = transaction;
			if (previousTransaction is not null)
			{
				previousTransaction.Dispose();
				if (previousTransaction.TransientUpdates.Any() 
					&& previousTransaction.TransientUpdates.Count != transaction.TransientUpdates.Count)
				{
					Update(); // Make sure to clear the transient updates that was not preserved
				}
			}

			return transaction; 
		}
	}

	private void EndUpdate(UpdateTransaction transaction, Func<CurrentMessage, MessageBuilder<TParent, TResult>> result)
	{
		lock (_gate)
		{
			if (_pendingUpdate == transaction)
			{
				_pendingUpdate = null;
				Update(result, CancellationToken.None);
			}
		}
	}

	private void EndUpdate(UpdateTransaction transaction)
	{
		lock (_gate)
		{
			if (_pendingUpdate == transaction)
			{
				_pendingUpdate = null;
				Update();
			}
		}
	}
}
