#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Sources;

internal enum FeedAsyncExecutionResult
{
	Success,
	Failed,
	Cancelled,
}

internal record ExecuteRequest(object Issuer, string Reason)
{
	/// <summary>
	/// Defined the axis used to flag the message as async (i.e. transient).
	/// This can be customized by sub-classer, in conjunction with the <see cref="AsyncValue"/>.
	/// </summary>
	internal virtual MessageAxis AsyncAxis => MessageAxis.Progress;

	/// <summary>
	/// Flags the message as async (i.e. transient).
	/// This can be customized by sub-classer, in conjunction with the <see cref="AsyncAxis"/>.
	/// </summary>
	internal virtual MessageAxisValue AsyncValue => MessageAxis.Progress.ToMessageValue(true);

	internal virtual bool IsAsync(IMessageEntry entry)
		=> entry.IsTransient;
}


//internal interface IFeedAsyncExtension
//{
//	bool CanProduceValue();
//}

//internal static class FeedAsyncContextExtensions
//{
//	private const string _refresh = "RefreshExtension";

//	public static void EnableRefresh<TParent, TResult>(this FeedAsyncSessionsManager<TParent, TResult> ctx, IRefreshableSource source, ISignal? externalRefreshSignal = null)
//	{
//		if (!ctx.Sessions.TryGetExtension(_refresh, out RefreshExtension refresh))
//		{
//			ctx.Sessions.SetExtension(_refresh, new RefreshExtension(ctx, source));
//		}
//	}

//	public static void DisableRefresh<TParent, TResult>(this FeedAsyncSessionsManager<TParent, TResult> ctx)
//	{
//		if (ctx.Sessions.TryRemoveExtension(_refresh, out RefreshExtension refresh))
//		{
//			refresh.Dispose();
//		}
//	}

//	private class RefreshExtension : IDisposable
//	{
//		private readonly SourceContext _context;
//		private readonly CancellationToken _ct;
//		private readonly FeedAsyncSession _session;

//		private RefreshToken _token;

//		public RefreshExtension(IRefreshableSource src, SourceContext context, CancellationToken ct, ISignal? externalRefreshSignal = null)
//		{
//			_context = context;
//			_ct = ct;
//			_token = RefreshToken.Initial(src, context);

//			_context.Requests<RefreshRequest>(
//				request =>
//				{
//					var refreshedToken = RefreshToken.InterlockedIncrement(ref _token);
//					request.Register(refreshedToken);
//					session.Reload(b => b.Refreshed(refreshedToken));
//				},
//				_ct);

//			if (externalRefreshSignal is not null)
//			{
//				AddRefreshSignal(externalRefreshSignal);
//			}
//		}

//		private void AddRefreshSignal(ISignal signal)
//			=> signal
//				.GetSource(_session.SourceContext, _ct.Token)
//				.ForEachAsync(() =>
//					{
//						RefreshToken.InterlockedIncrement(ref _token);
//						_session.Reload(b => b.Refreshed(_token));
//					},
//					_ct.Token);

//		/// <inheritdoc />
//		public void Dispose()
//			=> _ct.Cancel();
//	}



//}


internal static class AsyncFeedExtensions
{
	//public static void EnableRefresh(this FeedAsyncExecution execution, [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
	//{
	//	execution.GetExtension<FeedRefreshExtension>(new NamedRefreshableSource(caller, line), static (man, src) => new FeedRefreshExtension(man, src)).Enable();
	//}

	//internal record struct NamedRefreshableSource(string Caller, int Line) : IRefreshableSource;

	public static ValueTask<IImmutableList<TItem>> GetPaginated<TItem>(this FeedExecution execution, Func<PaginationBuilder<TItem>, PaginationConfiguration<TItem>> configure)
	{
		var dependency = execution.Session.GetShared(
			nameof(PaginationDependency<TItem>),
			static (session, key, _) => new PaginationDependency<TItem>(session, key),
			Unit.Default);

		return dependency.GetItems(execution, static (b, c) => c(b), configure);
	}

	public static ValueTask<IImmutableList<TItem>> GetPaginated<TItem>(this FeedExecution execution, string identifier, Func<PaginationBuilder<TItem>, PaginationConfiguration<TItem>> configure)
	{
		var dependency = execution.Session.GetShared(
			(typeof(PaginationDependency<TItem>), identifier),
			static (session, key, _) => new PaginationDependency<TItem>(session, key.identifier),
			Unit.Default);

		return dependency.GetItems(execution, static (b, c) => c(b), configure);
	}

	public static ValueTask<IImmutableList<TItem>> GetPaginated<TItem, TArgs>(this FeedExecution execution, Func<PaginationBuilder<TItem>, TArgs, PaginationConfiguration<TItem>> configure, TArgs args)
	{
		var dependency = execution.Session.GetShared(
			nameof(PaginationDependency<TItem>),
			static (session, key, _) => new PaginationDependency<TItem>(session, key),
			Unit.Default);

		return dependency.GetItems(execution, configure, args);
	}

	public static ValueTask<IImmutableList<TItem>> GetPaginated<TItem, TArgs>(this FeedExecution execution, string identifier, Func<PaginationBuilder<TItem>, TArgs, PaginationConfiguration<TItem>> configure, TArgs args)
	{
		var dependency = execution.Session.GetShared(
			(typeof(PaginationDependency<TItem>), identifier),
			static (session, key, state) => new PaginationDependency<TItem>(session, key.identifier),
			Unit.Default);

		return dependency.GetItems(execution, configure, args);
	}

	//public static async ValueTask<IImmutableList<TItem>> GetPaginatedByCursor<TItem>(this FeedAsyncExecution execution, AsyncFunc<PageRequest, IImmutableList<TItem>> getPage)
	//{

	//}

	//public static TCursor GetCurrentPageCursor<TCursor>(this FeedAsyncExecution execution, TCursor firstPage)
	//{

	//}

	//public static TCursor GetCurrentPageCursor<TCursor>(this FeedAsyncExecution execution, Func<TCursor> firstPage)
	//{

	//}

	//public static AsyncPagination<TItem> Pagination<TItem>(AsyncFunc<PageRequest, IImmutableList<TItem>> getPage)
	//	=> Pagination<TItem>(static b => b.ByIndex().GetPage(getPage));

	//public static AsyncPagination<TItem> Pagination<TItem>(Func<PaginationBuilder<TItem>, PaginationConfiguration<TItem>> paginationConfigurator)
	//{

	//}
}


//internal sealed class RefreshContextRequestToTriggerAdapter<TParent, TResult> : ILoadTrigger<TResult>
//{
//	private readonly IDisposable _registration;
//	private readonly Task? _externalRefreshTask;

//	private RefreshToken _token;
//	private bool _isContextRequestEnded;

//	/// <inheritdoc />
//	public event EventHandler<LoadRequest<TResult>>? LoadRequested;

//	public RefreshContextRequestToTriggerAdapter(IRefreshableSource src, FeedSession<TParent, TResult> target, ISignal? externalRefreshSignal = null)
//	{
//		var context = target.Context;
//		var ct = target.Token;

//		_token = RefreshToken.Initial(src, context);

//		_externalRefreshTask = externalRefreshSignal?.GetSource(context, ct).ForEachAsync(BeginRefresh, ct);
//		context.Requests<RefreshRequest>(Refresh, ct);
//		context.Requests<EndRequest>(
//			_ =>
//			{
//				_isContextRequestEnded = true;
//				TryComplete(null);
//			},
//			ct);

//		_externalRefreshTask?.ContinueWith(TryComplete, TaskContinuationOptions.ExecuteSynchronously);

//		_registration = target.RegisterReloadTrigger(this);
//	}

//	private void Refresh(RefreshRequest request)
//	{
//		var refreshedVersion = RefreshToken.InterlockedIncrement(ref _token);

//		request.Register(refreshedVersion);
//		RaiseRequested(refreshedVersion);
//	}
//	private void BeginRefresh(Unit _)
//	{
//		var refreshedVersion = RefreshToken.InterlockedIncrement(ref _token);

//		RaiseRequested(refreshedVersion);
//	}

//	void TryComplete(Task? _)
//	{
//		if (_externalRefreshTask is not { IsCompleted: false } && _isContextRequestEnded)
//		{
//			_registration.Dispose();
//		}
//	}

//	private void RaiseRequested(RefreshToken token)
//		=> LoadRequested?.Invoke(this, new LoadRequest<TResult> { Reason = b => b.Refreshed(token) });
//}

//internal sealed class TrackingStateStore : IStateStore
//{
//	private readonly IStateStore _inner;

//	public TrackingStateStore(IStateStore inner)
//	{
//		_inner = inner;
//	}

//	/// <inheritdoc />
//	public ValueTask DisposeAsync()
//		=> _inner.DisposeAsync();

//	/// <inheritdoc />
//	public bool HasSubscription<TSource>(TSource source)
//		where TSource : class
//		=> _inner.HasSubscription(source);

//	/// <inheritdoc />
//	public FeedSubscription<TValue> GetOrCreateSubscription<TSource, TValue>(TSource source)
//		where TSource : class, ISignal<Message<TValue>>
//		=> _inner.GetOrCreateSubscription<TSource, TValue>(source);

//	/// <inheritdoc />
//	public TState GetOrCreateState<TSource, TState>(TSource source, Func<SourceContext, TSource, TState> factory)
//		where TSource : class
//		where TState : IState
//		=> _inner.GetOrCreateState(source, factory);

//	/// <inheritdoc />
//	public TState CreateState<T, TState>(Option<T> initialValue, Func<SourceContext, Option<T>, TState> factory)
//		where TState : IState
//		=> _inner.CreateState(initialValue, factory);
//}

internal sealed class DynamicFeed<T> : IFeed<T>, IRefreshableSource
{
	private readonly AsyncFunc<Option<T>> _dataProvider;

	public DynamicFeed(AsyncFunc<T> dataProvider)
	{
		_dataProvider = async ct => Option.SomeOrNone(await dataProvider(ct));
	}

	public DynamicFeed(AsyncFunc<Option<T>> dataProvider)
	{
		_dataProvider = dataProvider;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
		=> new FeedSession<T>(this, context, _dataProvider, ct);
}
