using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Operators;

internal record FeedUpdate<T>(Predicate<MessageBuilder<T>> IsActive, Action<MessageBuilder<T>> Apply);

internal class UpdateFeed<T> : IFeed<T>
{
	private readonly AsyncEnumerableSubject<(UpdateAction action, FeedUpdate<T> update)> _updates = new(ReplayMode.Disabled);
	private readonly IFeed<T> _source;

	private enum UpdateAction
	{
		Add,
		Remove
	}

	public UpdateFeed(IFeed<T> source)
	{
		_source = source;
	}

	public async ValueTask Update(Predicate<MessageBuilder<T>> predicate, Action<MessageBuilder<T>> updater, CancellationToken ct)
		=> _updates.SetNext((UpdateAction.Add, new FeedUpdate<T>(predicate, updater)));

	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct)
		=> new UpdateFeedSource(this, context, ct);

	private class UpdateFeedSource : IAsyncEnumerable<Message<T>>
	{
		private readonly CancellationToken _ct;
		private readonly AsyncEnumerableSubject<Message<T>> _subject;
		private readonly MessageManager<T, T> _message;

		private ImmutableList<FeedUpdate<T>> _activeUpdates;
		private bool _isInError;

		public UpdateFeedSource(UpdateFeed<T> owner, SourceContext context, CancellationToken ct)
		{
			_ct = ct;
			_subject = new AsyncEnumerableSubject<Message<T>>(ReplayMode.EnabledForFirstEnumeratorOnly);
			_message = new MessageManager<T, T>(_subject.SetNext);
			_activeUpdates = ImmutableList<FeedUpdate<T>>.Empty;

			// mode=AbortPrevious => When we receive a new update, we can abort the update and start a new one
			owner._updates.ForEachAsync(OnUpdateReceived, ct);
			owner._source.GetSource(context, ct).ForEachAsync(OnParentUpdated, ct);
		}

		/// <inheritdoc />
		public IAsyncEnumerator<Message<T>> GetAsyncEnumerator(CancellationToken ct = default)
			=> _subject.GetAsyncEnumerator(ct);

		private void OnUpdateReceived((UpdateAction action, FeedUpdate<T> update) args)
		{
			lock (this)
			{
				switch (args.action)
				{
					case UpdateAction.Add:
						_activeUpdates = _activeUpdates.Add(args.update);
						if (!_isInError)
						{
							IncrementalUpdate(args.update);
							return;
						}
						break;

					case UpdateAction.Remove:
						_activeUpdates = _activeUpdates.Remove(args.update);
						break;

					default:
						throw new ArgumentOutOfRangeException($"Unkown update action '{args.action}'");
				}
				
				RebuildMessage(parentMsg: default);
			}
		}

		private void OnParentUpdated(Message<T> parentMsg)
		{
			lock (this)
			{
				RebuildMessage(parentMsg);
			}
		}

		private void IncrementalUpdate(FeedUpdate<T> update)
			=> _message.Update(
				(current, u) =>
				{
					try
					{
						var msg = current.With();
						u.Apply(new (msg.Get, ((IMessageBuilder)msg).Set));
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
				update,
				_ct);

		private void RebuildMessage(Message<T>? parentMsg)
		{
			_isInError = false;
			_message.Update(
				(current, parent) =>
				{
					try
					{
						var msg = current.WithParentOnly(parent);
						foreach (var update in _activeUpdates)
						{
							var builder = new MessageBuilder<T>(msg.Get, ((IMessageBuilder)msg).Set);
							if (update.IsActive(builder))
							{
								update.Apply(builder);
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
