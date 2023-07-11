using System;
using System.Linq;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Sources;

internal sealed partial class FeedSession<TResult> : FeedSession, IAsyncEnumerable<Message<TResult>>
{
	private readonly AsyncFunc<Option<TResult>> _mainAsyncAction;
	private readonly AsyncEnumerableSubject<Message<TResult>> _messages = new(ReplayMode.EnabledForFirstEnumeratorOnly);
	private readonly MessageManager<Unit, TResult> _message;

	public FeedSession(DynamicFeed<TResult> feed, SourceContext context, AsyncFunc<Option<TResult>> mainAsyncAction, CancellationToken ct)
		: base(feed, context, ct)
	{
		_mainAsyncAction = mainAsyncAction;

		_message = new MessageManager<Unit, TResult>(_messages.TrySetNext);

		RequestLoad(new ExecuteRequest(this, "Initial load")); // Initial load
	}

	#region Core executions chain managment (including Request support)
	private readonly object _requestsGate = new();
	private List<ExecuteRequest>? _pendingRequests;
	private ExecutionImpl? _currentExecution; // This the execution currently loading the data, 

	/// <inheritdoc />
	internal override void RequestLoad(ExecuteRequest request)
	{
		lock (_requestsGate)
		{
			(_pendingRequests ??= new()).Add(request);

			if (_currentExecution is { } current)
			{
				// Here we only request to the current to abort, we don't wait for it to complete.
				// It's its responsibility to call TryStartNext() when it's done.
				_ = current.DisposeAsync(); 
			}
			else
			{
				TryStartNext();
			}
		}
	}

	private bool TryStartNext()
	{
		lock (_requestsGate)
		{
			// When the current execution is about to complete, if we have pending request we start immediately the next execution.
			// This will prevent the current one to commit its result (as we already know that it's out-dated, we try to reduce the number of messages in feeds)
			// but it will also allow the new execution to preserve the transient axes (set using the updateTransaction.SetTransient, e.g. the IsTransient axis).
			if (_pendingRequests is { Count: > 0 } requests)
			{
				_pendingRequests = null; // Clear requests first to avoid cycling on same request if teh execution completes sync!
				_ = new ExecutionImpl(this, requests); // Note: we do NOT set this as _currentExecution. This is done by the new execution itself.

				return true;
			}
			else
			{
				return false;
			}
		}
	}
	#endregion

	#region Parent message support (i.e. FeedDependency)
	private readonly Dictionary<ISignal<IMessage>, IMessage> _parentMessages = new();
	private DynamicParentMessage _aggregatedParentMessage = DynamicParentMessage.Initial;
	private bool _isAggregatedParentMessageValid = true;

	internal override void UpdateParent(ISignal<IMessage> feed, IMessage message)
	{
		if (_parentMessages.TryGetValue(feed, out var previous) && previous == message)
		{
			return;
		}

		lock (_parentMessages)
		{
			_isAggregatedParentMessageValid = false;
			_parentMessages[feed] = message;
		}

		// Note: No needs to lock here to protect against _currentExecution being replaced,
		//		 the found 'current' will forward the update to the '_message' if Completed or disposed.
		//		 The only effect will be to produce more messages than we would like to.
		if (_currentExecution is { } current)
		{
			lock (current.StateGate)
			{
				if (current.State == ExecutionImpl.States.Loading)
				{
					return; // Nothing to do, the update will be processed when the main load action pushes a messages.
				}
			}
		}

		_message.Update(static (m, p) => m.With(p), GetParent(), default);
	}

	private IMessage GetParent()
	{
		lock (_parentMessages)
		{
			if (!_isAggregatedParentMessageValid)
			{
				_aggregatedParentMessage = _aggregatedParentMessage.With(_parentMessages.Values);
				_isAggregatedParentMessageValid = true;
			}

			return _aggregatedParentMessage;
		}
	}
	#endregion

	/// <inheritdoc />
	public IAsyncEnumerator<Message<TResult>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		=> _messages.GetAsyncEnumerator(cancellationToken);

	// ASYNC LOAD FOR PAGINATION!
	//private async Task<(TCursor? nextPage, PaginationInfo paginationState)> LoadPage(
	//	MessageManager<Unit, IImmutableList<TItem>>.UpdateTransaction message,
	//	TCursor cursor,
	//	TokenSet<RefreshToken>? refreshInfo,
	//	PaginationInfo pageInfo,
	//	uint? desiredPageSize,
	//	bool isFirstPage,
	//	CancellationToken ct)
	//{
	//	ValueTask<PageResult<TCursor, TItem>> pageTask = default;
	//	Exception? error = default;
	//	try
	//	{
	//		pageTask = _getPage(cursor, desiredPageSize, ct);
	//	}
	//	catch (OperationCanceledException) when (ct.IsCancellationRequested)
	//	{
	//		return (cursor, pageInfo);
	//	}
	//	catch (Exception e)
	//	{
	//		error = e;
	//		if (isFirstPage)
	//		{
	//			pageInfo = pageInfo with { HasMoreItems = false };
	//		}
	//	}

	//	if (error is not null)
	//	{
	//		message.Commit(msg => msg
	//			.With()
	//			.Error(error)
	//			.Refreshed(refreshInfo)
	//			.Paginated(pageInfo));

	//		return (cursor, pageInfo);
	//	}

	//	// If we are not yet and the 'dataTask' is really async, we need to send a new message flagged as transient
	//	// Note: This check is not "atomic", but it's valid as it only enables a fast path.
	//	if (!message.Local.Current.IsTransient)
	//	{
	//		// As lot of async methods are actually not really async but only re-scheduled,
	//		// we try to avoid the transient state by delaying a bit the message.
	//		for (var i = 0; !pageTask.IsCompleted && !ct.IsCancellationRequested && i < 5; i++)
	//		{
	//			await Task.Yield();
	//		}

	//		if (ct.IsCancellationRequested)
	//		{
	//			return (cursor, pageInfo);
	//		}

	//		// The 'valueProvider' is not completed yet, so we need to flag the current value as transient.
	//		// Note: We also provide the parentMsg which will be applied
	//		if (!pageTask.IsCompleted)
	//		{
	//			message.Update(msg =>
	//			{
	//				var result = msg
	//					.With()
	//					.Refreshed(refreshInfo)
	//					.Paginated(pageInfo);

	//				if (isFirstPage)
	//				{
	//					result.SetTransient(MessageAxis.Progress, MessageAxis.Progress.ToMessageValue(true));
	//				}
	//				else
	//				{
	//					result.SetTransient(MessageAxis.Pagination, MessageAxis.Pagination.ToMessageValue(pageInfo with { IsLoadingMoreItems = true }));
	//				}

	//				return result;
	//			});
	//		}
	//	}

	//	var items = (DifferentialImmutableList<TItem>)message.Local.Current.Data.SomeOrDefault(DifferentialImmutableList<TItem>.Empty);
	//	var changes = default(CollectionChangeSet?);
	//	var nextPage = cursor;
	//	try
	//	{
	//		var page = await pageTask.ConfigureAwait(false);
	//		var hadItems = items is { Count: > 0 };
	//		var hasItems = page.Items is { Count: > 0 };

	//		(items, changes) = (hadItems, hasItems, isFirstPage) switch
	//		{
	//			(false, false, _) => (DifferentialImmutableList<TItem>.Empty, CollectionChangeSet<TItem>.Empty),
	//			(false, true, _) => Reset(items, page.Items),
	//			(true, false, true) => Clear(items),
	//			(true, false, false) => (items, CollectionChangeSet<TItem>.Empty),
	//			(true, true, true) => Reset(items, page.Items),
	//			(true, true, false) => Add(items, page.Items),
	//		};

	//		nextPage = page.NextPage;
	//		if (nextPage is null)
	//		{
	//			pageInfo = pageInfo with { HasMoreItems = false };
	//		}
	//	}
	//	catch (OperationCanceledException) when (ct.IsCancellationRequested)
	//	{
	//		return (cursor, pageInfo);
	//	}
	//	catch (Exception e)
	//	{
	//		error = e;
	//		if (isFirstPage)
	//		{
	//			pageInfo = pageInfo with { HasMoreItems = false };
	//		}
	//	}

	//	message.Commit(
	//		msg =>
	//		{
	//			var builder = msg
	//				.With()
	//				.Refreshed(refreshInfo)
	//				.Paginated(pageInfo);

	//			if (error is not null)
	//			{
	//				builder.Error(error);
	//			}
	//			else if (items.Count == 0)
	//			{
	//				builder.Data(Option<IImmutableList<TItem>>.None(), changes).Error(null);
	//			}
	//			else
	//			{
	//				builder.Data(items, changes).Error(null);
	//			}

	//			return builder;
	//		});

	//	return (nextPage, pageInfo);
	//}

	//internal record struct DynamicDependencies(ICollection<DynamicFeedDependency> Feeds);

	//internal record struct DynamicFeedDependency(ISignal<IMessage> Feed, IMessage Message, ICollection<MessageAxis> TouchedAxes);
}
