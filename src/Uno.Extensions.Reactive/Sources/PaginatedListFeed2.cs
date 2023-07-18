using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Sources;

internal class PaginatedListFeed<TCursor, TItem> : IListFeed<TItem>
{
	private readonly TCursor _firstPage;
	private readonly GetPage<TCursor, TItem> _getPage;
	private readonly CollectionAnalyzer<TItem> _diffAnalyzer;

	public PaginatedListFeed(TCursor firstPage, GetPage<TCursor, TItem> getPage, ItemComparer<TItem> itemComparer = default)
	{
		_firstPage = firstPage;
		_getPage = getPage;
		_diffAnalyzer = ListFeed<TItem>.GetAnalyzer(itemComparer);
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<IImmutableList<TItem>>> GetSource(SourceContext context, CancellationToken ct)
	{
		var refreshRequests = new CoercingRequestManager<RefreshRequest, RefreshToken>(context, RefreshToken.Initial(this, context), ct);
		var pageRequests = new CoercingRequestManager<Core.PageRequest, PageToken>(context, PageToken.Initial(this, context), ct);
		var subject = new AsyncEnumerableSubject<Message<IImmutableList<TItem>>>(ReplayMode.EnabledForFirstEnumeratorOnly);
		var messages = new MessageManager<IImmutableList<TItem>>(subject.SetNext);

		_ = refreshRequests.ForEachAwaitWithCancellationAsync(Load, ConcurrencyMode.AbortPrevious, continueOnError: true, ct);

		return subject;

		async ValueTask Load(TokenSet<RefreshToken>? refresh, CancellationToken ct)
		{
			var cursor = _firstPage;
			var pageInfo = new PaginationInfo { HasMoreItems = true };
			var isFirstPage = true;

			await foreach (var pageToken in pageRequests.WithCancellation(ct).ConfigureAwait(false))
			{
				// The Progress is set to indeterminate only for the first page as we don't want the
				// FeedView to go in loading state when we are loading more items.
				using var message = isFirstPage
					? messages.BeginUpdate(ct, preservePendingAxes: MessageAxis.Progress)
					: messages.BeginUpdate(ct, preservePendingAxes: MessageAxis.Pagination);
				using var _ = context.AsCurrent();

				(cursor, pageInfo) = await LoadPage(
					message,
					cursor!,
					refresh,
					pageInfo with { Tokens = pageToken },
					pageRequests.LastRequest?.DesiredPageSize,
					isFirstPage,
					ct)
					.ConfigureAwait(false);

				// If we reached the end of the list, then exit
				if (!pageInfo.HasMoreItems)
				{
					return;
				}

				// Prepare the next token we will process
				pageRequests.MoveNext();
				refreshRequests.MoveNext();
				isFirstPage = false;
			}
		}
	}

	private async Task<(TCursor? nextPage, PaginationInfo paginationState)> LoadPage(
		MessageManager<Unit, IImmutableList<TItem>>.UpdateTransaction message,
		TCursor cursor,
		TokenSet<RefreshToken>? refreshInfo,
		PaginationInfo pageInfo,
		uint? desiredPageSize,
		bool isFirstPage,
		CancellationToken ct)
	{
		ValueTask<PageResult<TCursor, TItem>> pageTask = default;
		Exception? error = default;
		try
		{
			pageTask = _getPage(cursor, desiredPageSize, ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			return (cursor, pageInfo);
		}
		catch (Exception e)
		{
			error = e;
			if (isFirstPage)
			{
				pageInfo = pageInfo with { HasMoreItems = false };
			}
		}

		if (error is not null)
		{
			message.Commit(msg => msg
				.With()
				.Error(error)
				.Refreshed(refreshInfo)
				.Paginated(pageInfo));

			return (cursor, pageInfo);
		}

		// If we are not yet and the 'dataTask' is really async, we need to send a new message flagged as transient
		// Note: This check is not "atomic", but it's valid as it only enables a fast path.
		if (!message.Local.Current.IsTransient)
		{
			// As lot of async methods are actually not really async but only re-scheduled,
			// we try to avoid the transient state by delaying a bit the message.
			for (var i = 0; !pageTask.IsCompleted && !ct.IsCancellationRequested && i < 5; i++)
			{
				await Task.Yield();
			}

			if (ct.IsCancellationRequested)
			{
				return (cursor, pageInfo);
			}

			// The 'valueProvider' is not completed yet, so we need to flag the current value as transient.
			// Note: We also provide the parentMsg which will be applied
			if (!pageTask.IsCompleted)
			{
				message.Update(msg =>
				{
					var result = msg
						.With()
						.Refreshed(refreshInfo)
						.Paginated(pageInfo);

					if (isFirstPage)
					{
						result.SetTransient(MessageAxis.Progress, MessageAxis.Progress.ToMessageValue(true));
					}
					else
					{
						result.SetTransient(MessageAxis.Pagination, MessageAxis.Pagination.ToMessageValue(pageInfo with { IsLoadingMoreItems = true }));
					}

					return result;
				});
			}
		}

		var items = (DifferentialImmutableList<TItem>)message.Local.Current.Data.SomeOrDefault(DifferentialImmutableList<TItem>.Empty);
		var changes = default(CollectionChangeSet?);
		var nextPage = cursor;
		try
		{
			var page = await pageTask.ConfigureAwait(false);
			var hadItems = items is { Count: > 0 };
			var hasItems = page.Items is { Count: > 0 };

			(items, changes) = (hadItems, hasItems, isFirstPage) switch
			{
				(false, false, _) => (DifferentialImmutableList<TItem>.Empty, CollectionChangeSet<TItem>.Empty),
				(false, true, _) => Reset(items, page.Items),
				(true, false, true) => Clear(items),
				(true, false, false) => (items, CollectionChangeSet<TItem>.Empty),
				(true, true, true) => Reset(items, page.Items),
				(true, true, false) => Add(items, page.Items),
			};

			nextPage = page.NextPage;
			if (nextPage is null)
			{
				pageInfo = pageInfo with { HasMoreItems = false };
			}
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			return (cursor, pageInfo);
		}
		catch (Exception e)
		{
			error = e;
			if (isFirstPage)
			{
				pageInfo = pageInfo with { HasMoreItems = false };
			}
		}

		message.Commit(
			msg =>
			{
				var builder = msg
					.With()
					.Refreshed(refreshInfo)
					.Paginated(pageInfo);

				if (error is not null)
				{
					builder.Error(error);
				}
				else if(items.Count == 0)
				{
					builder.Data(Option<IImmutableList<TItem>>.None(), changes).Error(null);
				}
				else
				{
					builder.Data(items, changes).Error(null);
				}

				return builder;
			});

		return (nextPage, pageInfo);
	}

	private (DifferentialImmutableList<TItem> items, CollectionChangeSet changes) Reset(DifferentialImmutableList<TItem> current, IImmutableList<TItem> page)
	{
		var updated = new DifferentialImmutableList<TItem>(page);

		return (updated, _diffAnalyzer.GetResetChange(current, updated));
	}

	private (DifferentialImmutableList<TItem> items, CollectionChangeSet changes) Clear(DifferentialImmutableList<TItem> current)
	{
		return (DifferentialImmutableList<TItem>.Empty, _diffAnalyzer.GetResetChange(current, DifferentialImmutableList<TItem>.Empty));
	}

	private (DifferentialImmutableList<TItem> items, CollectionChangeSet changes) Add(DifferentialImmutableList<TItem> current, IImmutableList<TItem> page)
	{
		var updated = current.AddRange(page);
		var changes = _diffAnalyzer.GetChanges(RichNotifyCollectionChangedEventArgs.AddSome(page.AsUntypedList(), current.Count));

		return (updated, changes);
	}
}
