using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Operators;

internal interface IFeedUpdate<T>
{
	// Should we pass only the 'updatedParent', null is not updated, and remove the 'parentChanged' flag?
	bool IsActive(Message<T>? parent, bool parentChanged, IMessageEntry<T> message);

	// TODO: Should remove the 'parentChanged' flag ?
	void Apply(bool parentChanged, MessageBuilder<T, T> message);

	/// <summary>
	/// Determines whether this update can be removed from the active updates list
	/// after it has been applied. Called only when the parent source feed has completed
	/// (no more parent messages will arrive). When compacted, the update's effect
	/// remains baked into the current message but the record is released for GC.
	/// </summary>
	/// <returns>True if this update does not need to be retained for replay.</returns>
	bool IsCompactable() => false;
}

/// <summary>
/// An <see cref="IFeedUpdate{T}"/> that can undo its effect on the current message
/// when it is removed after having been compacted from the active updates list.
/// </summary>
internal interface IFeedRollbackableUpdate<T>
{
	void Rollback(MessageManager<T, T> message);
}

internal record FeedUpdate<T>(
	Func<Message<T>?, bool, IMessageEntry<T>, bool> IsActive,
	Action<bool, MessageBuilder<T, T>> Apply,
	Action<MessageManager<T, T>>? Rollback = null) : IFeedUpdate<T>, IFeedRollbackableUpdate<T>
{
	bool IFeedUpdate<T>.IsActive(Message<T>? parent, bool parentChanged, IMessageEntry<T> message) => IsActive(parent, parentChanged, message);
	void IFeedUpdate<T>.Apply(bool parentChanged, MessageBuilder<T, T> message) => Apply(parentChanged, message);

	bool IFeedUpdate<T>.IsCompactable()
		=> Rollback is not null;

	void IFeedRollbackableUpdate<T>.Rollback(MessageManager<T, T> message)
		=> Rollback?.Invoke(message);
}

internal sealed class UpdateFeed<T> : IFeed<T>
{
	private readonly AsyncEnumerableSubject<(IFeedUpdate<T>[]? added, IFeedUpdate<T>[]? removed)> _updates = new(ReplayMode.Disabled);
	private readonly IFeed<T> _source;
	private readonly Predicate<Message<T>>? _waitForParent;

	public UpdateFeed(IFeed<T> source, Predicate<Message<T>>? waitForParent = null)
	{
		_source = source;
		_waitForParent = waitForParent;
	}

	public async ValueTask Update(Func<Message<T>?, bool, IMessageEntry<T>, bool> predicate, Action<bool, MessageBuilder<T, T>> updater, CancellationToken ct)
	{
		var update = new FeedUpdate<T>(predicate, updater);
		_updates.SetNext((new[] { update }, null));
	}

	internal void Add(IFeedUpdate<T> update)
		=> _updates.SetNext((new[] { update }, null));

	internal void Replace(IFeedUpdate<T> previous, IFeedUpdate<T> current)
		=> _updates.SetNext((new[] { current }, new[] { previous }));

	internal void Remove(IFeedUpdate<T> update)
		=> _updates.SetNext((null, new[] { update }));

	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct)
		=> new UpdateFeedSource(this, context, ct);

	private class UpdateFeedSource : IAsyncEnumerable<Message<T>>
	{
		private readonly UpdateFeed<T> _owner;
		private readonly CancellationToken _ct;
		private readonly AsyncEnumerableSubject<Message<T>> _subject;
		private readonly MessageManager<T, T> _message;

		private ImmutableList<IFeedUpdate<T>> _activeUpdates;
		private ImmutableDictionary<IFeedUpdate<T>, IFeedRollbackableUpdate<T>> _compactedUpdates;
		private bool _isInError;
		private bool _isParentReady;
		private bool _isParentCompleted;

		public UpdateFeedSource(UpdateFeed<T> owner, SourceContext context, CancellationToken ct)
		{
			_owner = owner;
			_ct = ct;
			_subject = new AsyncEnumerableSubject<Message<T>>(ReplayMode.EnabledForFirstEnumeratorOnly);
			_message = new MessageManager<T, T>(_subject.SetNext);
			_activeUpdates = ImmutableList<IFeedUpdate<T>>.Empty;
			_compactedUpdates = ImmutableDictionary<IFeedUpdate<T>, IFeedRollbackableUpdate<T>>.Empty;
			_isParentReady = owner._waitForParent is null;

			// mode=AbortPrevious => When we receive a new update, we can abort the update and start a new one
			owner._updates.ForEachAsync(OnUpdateReceived, ct);
			context
				.GetOrCreateSource(owner._source)
				.ForEachAsync(OnParentUpdated, ct)
				.ContinueWith(
					_ =>
					{
						lock (this)
						{
							_isParentCompleted = true;
							TryCompact();
						}
					},
					TaskContinuationOptions.ExecuteSynchronously);
		}

		/// <inheritdoc />
		public IAsyncEnumerator<Message<T>> GetAsyncEnumerator(CancellationToken ct = default)
			=> _subject.GetAsyncEnumerator(ct);

		private void OnUpdateReceived((IFeedUpdate<T>[]? added, IFeedUpdate<T>[]? removed) args)
		{
			lock (this)
			{
				if (!_isParentReady)
				{
					return;
				}

				bool needsUpdate = false, canDoIncrementalUpdate = !_isInError;
				if (args.removed is { Length: > 0 } removed)
				{
					var updates = _activeUpdates.RemoveRange(removed);
					if (_activeUpdates != updates)
					{
						needsUpdate = true;
						canDoIncrementalUpdate = false;
						_activeUpdates = updates;
					}

					// Handle Remove of previously compacted updates
					foreach (var r in removed)
					{
						if (_compactedUpdates.TryGetValue(r, out var rollbackable))
						{
							_compactedUpdates = _compactedUpdates.Remove(r);
							needsUpdate = true;
							canDoIncrementalUpdate = false;
							rollbackable.Rollback(_message);
						}
					}
				}

				if (args.added is { Length: > 0 } added)
				{
					var updates = _activeUpdates.AddRange(added);
					if (_activeUpdates != updates)
					{
						needsUpdate = true;
						_activeUpdates = updates;

						if (canDoIncrementalUpdate)
						{
							IncrementalUpdate(added);
							TryCompact();
							return;
						}
					}
				}

				if (needsUpdate)
				{
					RebuildMessage(parentMsg: default);
					TryCompact();
				}
			}
		}

		private void OnParentUpdated(Message<T> parentMsg)
		{
			lock (this)
			{
				if (!_isParentReady)
				{
					_isParentReady = _owner._waitForParent!.Invoke(parentMsg);
				}

				RebuildMessage(parentMsg);
			}
		}

		private void IncrementalUpdate(IFeedUpdate<T>[] updates)
		{
			_message.Update(
				(current, u) =>
				{
					try
					{
						var msg = current.With();
						foreach (var update in u)
						{
							update.Apply(false, msg);
						}
						return msg;
					}
					catch (Exception error)
					{
						_isInError = true;
						return current
							.WithParentOnly(null)
							.Data(Option<T>.Undefined())
							.Error(error);
					}
				},
				updates,
				_ct);
		}

		/// <summary>
		/// Removes compactable updates from _activeUpdates, keeping their
		/// effect baked into the current message. Must be called under lock(this).
		/// </summary>
		private void TryCompact()
		{
			if (!_isParentCompleted || _activeUpdates.IsEmpty)
			{
				return;
			}

			var remainingUpdates = _activeUpdates.ToBuilder();
			foreach (var update in _activeUpdates)
			{
				if (update.IsCompactable())
				{
					remainingUpdates.Remove(update);

					if (update is IFeedRollbackableUpdate<T> rollbackable)
					{
						_compactedUpdates = _compactedUpdates.Add(update, rollbackable);
					}
				}
			}

			if (remainingUpdates.Count != _activeUpdates.Count)
			{
				_activeUpdates = remainingUpdates.ToImmutable();
			}
		}

		private void RebuildMessage(Message<T>? parentMsg)
		{
			_isInError = false;
			_message.Update(
				(current, parent) =>
				{
					try
					{
						var parentChanged = parent is not null;
						var msg = current.WithParentOnly(parent);
						foreach (var update in _activeUpdates)
						{
							if (update.IsActive(msg.Parent, parentChanged, msg))
							{
								update.Apply(parentChanged, msg);
							}
							else
							{
								_activeUpdates = _activeUpdates.Remove(update);
							}
						}

						return msg;
					}
					catch (Exception error)
					{
						_isInError = true;
						return current
							.WithParentOnly(parent)
							.Data(Option<T>.Undefined())
							.Error(error);
					}
				},
				parentMsg,
				_ct);
		}
	}
}
