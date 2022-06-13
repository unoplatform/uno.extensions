using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Reactive.Core.FeedHelper;

namespace Uno.Extensions.Reactive.Sources;

internal class CoercingRequestManager<TRequest, TToken> : IAsyncEnumerable<TokenCollection<TToken>>
	where TRequest : IContextRequest<TToken>
	where TToken : class, IToken<TToken>
{
	private readonly AsyncEnumerableSubject<TokenCollection<TToken>> _tokens;
	private readonly CancellationToken _ct;
	private readonly Task _task;

	private TToken _current;
	private TToken? _lastRequested;

	public CoercingRequestManager(SourceContext context, TToken initial, CancellationToken ct, bool autoPublishInitial = true)
	{
		_current = initial; // The page that is being loaded or will be load on next request
		_ct = ct;

		_tokens = new AsyncEnumerableSubject<TokenCollection<TToken>>(ReplayMode.EnabledForFirstEnumeratorOnly);
		if (autoPublishInitial)
		{
			_tokens.SetNext(initial);
			_lastRequested = initial;
		}

		ct.Register(() => "".ToString());
		_task = context.Requests<TRequest>().ForEachAsync(OnRequest, ct);
		ct.Register(_tokens.TryComplete);
	}

	public TToken Current => _current;

	/// <inheritdoc />
	public IAsyncEnumerator<TokenCollection<TToken>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		=> _tokens.GetAsyncEnumerator(cancellationToken);

	/// <summary>
	/// Move to the next token that is going to be used for subsequent request.
	/// </summary>
	/// <remarks>
	/// The given token will be published by the <see cref="IAsyncEnumerable{T}"/> only on next request received.
	/// </remarks>
	public void MoveNext()
		=> _current = _current.Next();

	private void OnRequest(TRequest request)
	{
		if (_ct.IsCancellationRequested)
		{
			return;
		}

		// If the currentPage has not been requested yet, then request it!
		if (Interlocked.Exchange(ref _lastRequested, _current) != _current)
		{
			_tokens.TrySetNext(_current);
		}

		request.Register(_current);
	}
}

internal class SequentialRequestManager<TRequest, TToken> : IAsyncEnumerable<TokenCollection<TToken>>
	where TRequest : IContextRequest<TToken>
	where TToken : class, IToken<TToken>
{
	private readonly SourceContext _context;
	private readonly TToken _initial;
	private readonly CancellationToken _ct;
	private readonly bool _autoPublishInitial;

	public SequentialRequestManager(SourceContext context, TToken initial, CancellationToken ct, bool autoPublishInitial = true)
	{
		_context = context;
		_initial = initial;
		_ct = ct;
		_autoPublishInitial = autoPublishInitial;
	}

	/// <inheritdoc />
	public async IAsyncEnumerator<TokenCollection<TToken>> GetAsyncEnumerator(CancellationToken ct)
	{
		ct = CancellationTokenSource.CreateLinkedTokenSource(_ct, ct).Token;

		var token = _initial;

		if (_autoPublishInitial)
		{
			yield return token;
		}

		await foreach (var _ in _context.Requests<TRequest>().WithCancellation(ct).ConfigureAwait(false))
		{
			if (ct.IsCancellationRequested)
			{
				yield break;
			}

			yield return token = token.Next();
		}
	}
}

internal class PaginatedListFeed<TCursor, TItem> : IListFeed<TItem>, IRefreshableSource, IPaginatedSource
{
	private readonly TCursor _firstPage;
	private readonly AsyncFunc<TCursor, Page<TCursor, TItem>> _loadPage;
	private readonly CollectionAnalyzer<TItem> _diffAnalyzer;

	public PaginatedListFeed(TCursor firstPage, AsyncFunc<TCursor, Page<TCursor, TItem>> loadPage)
	{
		_firstPage = firstPage;
		_loadPage = loadPage;
		_diffAnalyzer = new CollectionAnalyzer<TItem>(default);
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<IImmutableList<TItem>>> GetSource(SourceContext context, CancellationToken ct)
	{
		//var pages = new AsyncEnumerableSubject<TokenCollection<PageToken>>(ReplayMode.EnabledForFirstEnumeratorOnly);
		//var currentPage = PageToken.Initial(this, context); // The page that is being loaded or will be load on next request
		//var lastRequestedPage = default(PageToken); 

		//_ = context.Requests<PageRequest>().ForEachAsync(RequestNextPage, ct);
		//ct.Register(pages.TryComplete);

		//void RequestNextPage(PageRequest request)
		//{
		//	if (ct.IsCancellationRequested)
		//	{
		//		return;
		//	}

		//	// If the currentPage has not been requested yet, then request it!
		//	if (Interlocked.Exchange(ref lastRequestedPage, currentPage) != currentPage)
		//	{
		//		pages.TrySetNext(currentPage);
		//	}

		//	request.Register(currentPage);
		//}

		var refreshRequests = new SequentialRequestManager<RefreshRequest, RefreshToken>(context, RefreshToken.Initial(this, context), ct);
		var pageRequests = new CoercingRequestManager<PageRequest, PageToken>(context, PageToken.Initial(this, context), ct);
		var subject = new AsyncEnumerableSubject<Message<IImmutableList<TItem>>>(ReplayMode.EnabledForFirstEnumeratorOnly);
		var messages = new MessageManager<IImmutableList<TItem>>(subject.SetNext);

		_ = refreshRequests.ForEachAwaitWithCancellationAsync(Load, ConcurrencyMode.AbortPrevious, continueOnError: true, ct);

		return subject;

		async ValueTask Load(TokenCollection<RefreshToken>? refresh, CancellationToken ct)
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

				(cursor, pageInfo) = await LoadPage(message, cursor!, refresh, pageInfo with { Tokens = pageToken }, isFirstPage, ct);

				// If we reached the end of the list, then exit
				if (!pageInfo.HasMoreItems)
				{
					return;
				}

				// Prepare the next token we will process
				pageRequests.MoveNext();
				isFirstPage = false;
			}
		}
	}

	private async Task<(TCursor? nextPage, PaginationInfo paginationState)> LoadPage(
		MessageManager<Unit, IImmutableList<TItem>>.UpdateTransaction message,
		TCursor cursor,
		TokenCollection<RefreshToken>? refreshInfo,
		PaginationInfo pageInfo,
		bool isFirstPage,
		CancellationToken ct)
	{
		ValueTask<Page<TCursor, TItem>> pageTask = default;
		Exception? error = default;
		try
		{
			pageTask = _loadPage(cursor, ct);
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
		var nextPage = default(TCursor?);
		try
		{
			var page = await pageTask;
			var hadItems = items is { Count: > 0 };
			var hasItems = page.Items is { Count: > 0 };

			(items, changes) = (hadItems, hasItems, isFirstPage) switch
			{
				(false, false, _) => (DifferentialImmutableList<TItem>.Empty, CollectionChangeSet.Empty),
				(false, true, _) => Reset(items, page.Items),
				(true, false, true) => Clear(items),
				(true, false, false) => (items, CollectionChangeSet.Empty),
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

	//private async IAsyncEnumerable<TokenCollection<RefreshToken>?> LoadRequests(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	//{
	//	var token = RefreshToken.Initial(this, context);

	//	yield return null;

	//	await foreach (var _ in context.Requests<RefreshRequest>().WithCancellation(ct).ConfigureAwait(false))
	//	{
	//		yield return RefreshToken.InterlockedIncrement(ref token);
	//	}
	//}

	private (DifferentialImmutableList<TItem> items, CollectionChangeSet changes) Reset(DifferentialImmutableList<TItem> current, IImmutableList<TItem> page)
	{
		var updated = new DifferentialImmutableList<TItem>(page);

		return (updated, ((CollectionAnalyzer)_diffAnalyzer).GetResetChange(current, updated));
	}

	private (DifferentialImmutableList<TItem> items, CollectionChangeSet changes) Clear(DifferentialImmutableList<TItem> current)
	{
		return (DifferentialImmutableList<TItem>.Empty, ((CollectionAnalyzer)_diffAnalyzer).GetResetChange(current, Array.Empty<TItem>()));
	}

	private (DifferentialImmutableList<TItem> items, CollectionChangeSet changes) Add(DifferentialImmutableList<TItem> current, IImmutableList<TItem> page)
	{
		var updated = current.AddRange(page);
		var changes = _diffAnalyzer.GetChanges(RichNotifyCollectionChangedEventArgs.AddSome(page.AsUntypedList(), current.Count));

		return (updated, changes);
	}
}

public record struct Page<TCursor, TItem>(IImmutableList<TItem> Items, TCursor? NextPage)
{
	public static Page<TCursor, TItem> Empty => new(ImmutableList<TItem>.Empty, NextPage: default);
}
