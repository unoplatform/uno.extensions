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
	bool IsActive(bool parentChanged, MessageBuilder<T, T> message);

	void Apply(bool parentChanged, MessageBuilder<T, T> message);
}

internal record FeedUpdate<T>(Func<bool, MessageBuilder<T, T>, bool> IsActive, Action<bool, MessageBuilder<T, T>> Apply) : IFeedUpdate<T>
{
	bool IFeedUpdate<T>.IsActive(bool parentChanged, MessageBuilder<T, T> message) => IsActive(parentChanged, message);
	void IFeedUpdate<T>.Apply(bool parentChanged, MessageBuilder<T, T> message) => Apply(parentChanged, message);
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

	public async ValueTask Update(Func<bool, MessageBuilder<T, T>, bool> predicate, Action<bool, MessageBuilder<T, T>> updater, CancellationToken ct)
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
		private bool _isInError;
		private bool _isParentReady;

		public UpdateFeedSource(UpdateFeed<T> owner, SourceContext context, CancellationToken ct)
		{
			_owner = owner;
			_ct = ct;
			_subject = new AsyncEnumerableSubject<Message<T>>(ReplayMode.EnabledForFirstEnumeratorOnly);
			_message = new MessageManager<T, T>(_subject.SetNext);
			_activeUpdates = ImmutableList<IFeedUpdate<T>>.Empty;
			_isParentReady = owner._waitForParent is null;

			// mode=AbortPrevious => When we receive a new update, we can abort the update and start a new one
			owner._updates.ForEachAsync(OnUpdateReceived, ct);
			context.GetOrCreateSource(owner._source).ForEachAsync(OnParentUpdated, ct);
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

				var canDoIncrementalUpdate = !_isInError;
				if (args.removed is { Length: > 0 } removed)
				{
					canDoIncrementalUpdate = false;
					_activeUpdates = _activeUpdates.RemoveRange(removed);
				}

				if (args.added is { Length: > 0 } added)
				{
					_activeUpdates = _activeUpdates.AddRange(added);

					if (canDoIncrementalUpdate)
					{
						IncrementalUpdate(added);
						return;
					}
				}

				RebuildMessage(parentMsg: default);
			}
		}

		private void OnParentUpdated(Message<T> parentMsg)
		{
			lock (this)
			{
				if (!_isParentReady)
				{
					_isParentReady |= _owner._waitForParent!.Invoke(parentMsg);
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
							if (update.IsActive(parentChanged, msg))
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
