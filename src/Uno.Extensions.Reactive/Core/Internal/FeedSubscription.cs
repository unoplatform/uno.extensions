using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

// This class is responsible to hold the subscription made on a feed for a given context.
// It makes sure to replay the last messages to the subscriber, so we create only one subscription to the source feed,
// and we ensure that all dependent feeds that are using the same context will receive the same message instance.
internal class FeedSubscription<T> : IAsyncDisposable, ISourceContextOwner
{
	private readonly CompositeRequestSource _requests = new();

	private readonly ISignal<Message<T>> _feed;
	private readonly SourceContext _rootContext;
	private readonly SourceContext _context;
	private readonly ReplayOneAsyncEnumerable<Message<T>> _messages;

	public FeedSubscription(ISignal<Message<T>> feed, SourceContext rootContext)
	{
		_feed = feed;
		_rootContext = rootContext;
		_context = rootContext.CreateChild(this, _requests);
		_messages = new ReplayOneAsyncEnumerable<Message<T>>(
			feed.GetSource(_context),
			isInitialSyncValuesSkippingAllowed: true);
	}

	string ISourceContextOwner.Name => $"Sub on '{_feed}' for ctx '{_context.Parent!.Owner.Name}'.";

	IDispatcher? ISourceContextOwner.Dispatcher => null;

	internal Message<T> Current => _messages.TryGetCurrent(out var value) ? value : Message<T>.Initial;

	public IDisposable UpdateMode(SubscriptionMode mode)
	{
		// Not supported yet.
		// Here we should compute the stricter mode
		return Disposable.Empty;
	}

	public async IAsyncEnumerable<Message<T>> GetMessages(SourceContext subscriberContext, [EnumeratorCancellation] CancellationToken ct)
	{
		if (subscriberContext != _rootContext)
		{
			_requests.Add(subscriberContext.RequestSource, ct);
		}

		var isFirstMessage = true;
		await foreach (var msg in _messages.WithCancellation(ct).ConfigureAwait(false))
		{
			if (isFirstMessage)
			{
				// We make sure that even if we are replaying a previous message, the changes collection contains all keys.
				isFirstMessage = false;
				yield return Message<T>.Initial.OverrideBy(msg);
			}
			else
			{
				yield return msg;
			}
		}

		if (isFirstMessage)
		{
			this.Log().LogWarning(
				$"The source feed ({_feed}) completed the enumeration but didn't produced any message. "
				+ "All feeds must send at least one initial message!");

			yield return Message<T>.Initial;
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _context.DisposeAsync().ConfigureAwait(false);
		_requests.Dispose();
		await _messages.DisposeAsync().ConfigureAwait(false);
	}
}
