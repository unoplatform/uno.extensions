using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Sources.Pagination;
using Uno.Extensions.Threading;

namespace Uno.Extensions.Reactive.Sources;

// Note: This is an initial naive implementation, we should split the "Cursor" and the "Collection" itself.
//		 Doing this would allow us to implement custom pagination logic, but also let applications extract some meta-data (like total page count) and use it in their own strcuture.
internal class PaginationDependency<TItem> : IDependency
{
	private readonly FastAsyncLock _gate = new();

	private readonly FeedSession _session;
	private readonly string _identifier;
	private readonly CancellationTokenSource _ct;

	private PageToken _token;
	private PaginationInfo _pageInfo;
	private IPageEnumerator<TItem>? _pages;

	private bool _gotItems;
	private DifferentialImmutableList<TItem> _items = DifferentialImmutableList<TItem>.Empty;

	public PaginationDependency(FeedSession session, string identifier)
	{
		_session = session;
		_identifier = identifier;

		_ct = CancellationTokenSource.CreateLinkedTokenSource(session.Token);
		_token = PageToken.Initial(_session.Owner, _session.Context);
		_pageInfo = new() { HasMoreItems = true };

		_session.Context.Requests<Uno.Extensions.Reactive.Core.PageRequest>(OnPageRequested, _ct.Token);
		_session.Context.Requests<Uno.Extensions.Reactive.Core.EndRequest>(_ => _session.UnRegisterDependency(this), _ct.Token);
		_session.RegisterDependency(this);
	}

	private void OnPageRequested(Uno.Extensions.Reactive.Core.PageRequest req)
	{
		// First we issue a new token
		PageToken current, next;
		do
		{
			current = _token;
			next = current.Next();
		} while (Interlocked.CompareExchange(ref _token, next, current) != current);

		// Finally we let know to the requester the token it has to listen for,
		// then we request to the feed to re-invoke the factory so we will be able to load the next page.
		req.Register(next);
		_session.Execute(new PaginationExecuteRequest(this, req.DesiredPageSize, next));
	}

	/// <inheritdoc />
	async ValueTask IDependency.OnExecuting(FeedExecution execution, CancellationToken ct)
	{
		_gotItems = false;

		if (FindPaginationRequest(execution).request is { } req)
		{
			// Note: If the user does not invoke the GetItems during the loading process,
			//		 we still push token token as we must ensure that the next page requester does not remain in loading state.

			Update(_pageInfo with { Tokens = req.Token });

			// Push the _pageInfo in loading so the token will be propagated even in transient messages.
			execution.Enqueue(m => m.Paginated(_pageInfo));
		}
	}

	/// <inheritdoc />
	async ValueTask IDependency.OnExecuted(FeedExecution execution, FeedExecutionResult result, CancellationToken ct)
	{
		// Push again the _pageInfo as it might have been updated by the GetItems / Request.SetAsync.
		execution.Enqueue(m => m.Paginated(_pageInfo));

		if (!_gotItems)
		{
			this.Log().Warn($"A page has been requested for {_identifier} and a load has been triggered, but the items has not been fetched by the 'load method'.");
		}
	}

	public async ValueTask<IImmutableList<TItem>> GetItems<TArgs>(FeedExecution exec, Func<PaginationBuilder<TItem>, TArgs, PaginationConfiguration<TItem>> configure, TArgs args)
	{
		if (exec.Token.IsCancellationRequested)
		{
			Debug.Fail("The pagination can be used only while executing the async operation of a Feed.");
			if (this.Log().IsEnabled(LogLevel.Warning)) this.Log().Warn("The pagination can be used only while executing the async operation of a Feed.");

			return _items;
		}

		_gotItems = true;

		using (await _gate.LockAsync(_ct.Token))
		{
			var (isPagination, req) = FindPaginationRequest(exec);
			if (_pages is null // Initial load (req is then expected to be an InitialLoadRequest)
				|| !isPagination) // Another dependency changed, we need to restart at page 0!
			{
				_pages = configure(new PaginationBuilder<TItem>(), args).GetEnumerator(_session.Token); // We use the Enumerator across multiple execution, we need to use the _session.Token.
				Update(_pageInfo with { HasMoreItems = true, IsLoadingMoreItems = false });
				_items = DifferentialImmutableList<TItem>.Empty;
			}
			else if (req is null)
			{
				// This a pagination request for another paginated list (multiple paginated collection within the same feed?!)
				// We don't have to update our pagination info, nor to reset the page index.
				return _items;
			}

			if (await _pages.MoveNextAsync(req?.DesiredPageSize))
			{
				// No needs to configure the _pageInfo, it has been done in the OnLoading method.
				_items = _items.AddRange(_pages.Current);
			}
			else
			{
				Update(_pageInfo with { HasMoreItems = false });
			}

			return _items;
		}
	}

	private (bool isPagination, PaginationExecuteRequest? request) FindPaginationRequest(FeedExecution execution)
	{
		var isPagination = true;
		foreach (var request in execution.Requests)
		{
			if (request is PaginationExecuteRequest paginationRequest)
			{
				if (paginationRequest.Issuer == this)
				{
					return (isPagination, paginationRequest);
				}
			}
			else
			{
				isPagination = false;
			}
		}

		return (isPagination, null);
	}

	private void Update(PaginationInfo info)
	{
		if (!_pageInfo.Equals(info))
		{
			_pageInfo = info;
		}
	}

	private record PaginationExecuteRequest(PaginationDependency<TItem> issuer, uint? DesiredPageSize, PageToken Token) : ExecuteRequest(issuer, $"Page of {typeof(TItem)} requested")
	{
		/// <inheritdoc />
		internal override MessageAxis AsyncAxis => MessageAxis.Pagination;

		/// <inheritdoc />
		internal override MessageAxisValue AsyncValue => MessageAxis.Pagination.ToMessageValue(issuer._pageInfo with { IsLoadingMoreItems = true }); // This is not immutable !!! :/

		/// <inheritdoc />
		internal override bool IsAsync(IMessageEntry entry)
			=> entry.GetPaginationInfo()?.IsLoadingMoreItems ?? false;
	}
}
