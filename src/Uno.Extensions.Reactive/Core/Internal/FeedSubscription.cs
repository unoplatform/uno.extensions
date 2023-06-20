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
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal class FeedSubscription
{
	/// <summary>
	/// Determines if we allow FeedSubscription to bypass multiple initial sync values (cf. remarks for more details).
	/// </summary>
	/// <remarks>
	/// This is almost only a test case, but if a source feeds enumerates multiple values at startup (i.e. in the GetSource),
	/// the <see cref="ReplayOneAsyncEnumerable{T}"/> which backs the <see cref="FeedSubscription{T}"/> might miss some of those values to replay only the last one.
	/// </remarks>
	public static bool IsInitialSyncValuesSkippingAllowed { get; set; } = true;
}

internal class FeedSubscription<T> : IAsyncDisposable, ISourceContextOwner
{
	private readonly ISignal<Message<T>> _feed;
	private readonly SourceContext _rootContext;
	private readonly CompositeRequestSource _requests = new();
	private readonly SourceContext _context;
	private readonly ReplayOneAsyncEnumerable<Message<T>> _source;

	public FeedSubscription(ISignal<Message<T>> feed, SourceContext rootContext)
	{
		_feed = feed;
		_rootContext = rootContext;
		_context = rootContext.CreateChild(this, _requests);
		_source = new ReplayOneAsyncEnumerable<Message<T>>(
			feed.GetSource(_context),
			isInitialSyncValuesSkippingAllowed: FeedSubscription.IsInitialSyncValuesSkippingAllowed);
	}

	string ISourceContextOwner.Name => $"Sub on '{_feed}' for ctx '{_context.Parent!.Owner.Name}'.";

	IDispatcher? ISourceContextOwner.Dispatcher => null;

	internal Message<T> Current => _source.TryGetCurrent(out var value) ? value : Message<T>.Initial;

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
		await foreach (var msg in _source.WithCancellation(ct).ConfigureAwait(false))
		{
			if (isFirstMessage)
			{
				// We make sure that even if we replaying a previous message, the changes collection contains all keys.
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
				"The source feed completed the enumeration but didn't produced any message. "
				+ "All must send at least one initial message!");

			yield return Message<T>.Initial;
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _context.DisposeAsync();
		_requests.Dispose();
		await _source.DisposeAsync();
	}
}
