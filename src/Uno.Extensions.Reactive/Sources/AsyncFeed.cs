using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Reactive.Core.FeedHelper;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class AsyncFeed<T> : IFeed<T>
{
	private readonly ISignal? _refresh;
	private readonly AsyncFunc<Option<T>> _dataProvider;

	public AsyncFeed(AsyncFunc<T> dataProvider, ISignal? refresh = null)
	{
		_dataProvider = async ct => Option.SomeOrNone(await dataProvider(ct));
		_refresh = refresh;
	}

	public AsyncFeed(AsyncFunc<Option<T>> dataProvider, ISignal? refresh = null)
	{
		_dataProvider = dataProvider;
		_refresh = refresh;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		var loadRequests = new AsyncEnumerableSubject<RefreshToken>(ReplayMode.EnabledForFirstEnumeratorOnly);
		var current = RefreshToken.Initial(this, context);

		// Request initial load (without refresh)
		loadRequests.SetNext(current);

		// Then subscribe to refresh sources
		var localRefreshTask = _refresh?.GetSource(context, ct).ForEachAsync(BeginRefresh, ct);
		var contextRefreshEnded = false;
		context.Requests<RefreshRequest>(Refresh, ct);
		context.Requests<EndRequest>(_ =>
			{
				contextRefreshEnded = true;
				TryComplete(null);
			},
			ct);

		localRefreshTask?.ContinueWith(TryComplete, TaskContinuationOptions.ExecuteSynchronously);

		void Refresh(RefreshRequest request)
		{
			var refreshedVersion = RefreshToken.InterlockedIncrement(ref current);

			request.Register(refreshedVersion);
			loadRequests.SetNext(refreshedVersion);
		}
		void BeginRefresh(Unit _)
		{
			var refreshedVersion = RefreshToken.InterlockedIncrement(ref current);

			loadRequests.SetNext(refreshedVersion);
		}

		void TryComplete(Task? _)
		{
			if (localRefreshTask is not { IsCompleted: false } && contextRefreshEnded)
			{
				loadRequests.TryComplete();
			}
		}

		// Note: We prefer to manually enumerate the version instead of using the ForEachAwaitWithCancellationAsync
		//		 so we have a better control of when we do cancel the 'loadToken' (ak.a. 'previousLoad')

		var subject = new AsyncEnumerableSubject<Message<T>>(ReplayMode.EnabledForFirstEnumeratorOnly);
		var message = new MessageManager<T>(subject.SetNext);
		var loadToken = default(CancellationTokenSource);
		var load = default(Task);

		BeginEnumeration();

		return subject;

		async void BeginEnumeration()
		{
			try
			{
				var loadRequest = loadRequests.GetAsyncEnumerator(ct);
				while (await loadRequest.MoveNextAsync(ct).ConfigureAwait(false))
				{
					var previousLoad = loadToken;
					// Capture the version so if while loop exit we still have the right value.
					// We also make sure to convert it only once in TokenSet so we keep the same instance in case of multiple set by the InvokeAsync
					var refreshToken = (TokenSet<RefreshToken>)loadRequest.Current;
					loadToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
					load = InvokeAsync(
						message,
						null,
						_dataProvider,
						b => b.Refreshed(refreshToken),
						context,
						loadToken.Token);

					// We prefer to cancel the previous projection only AFTER so we are able to keep existing transient axes (cf. message.BeginTransaction)
					// This will not cause any concurrency issue since a transaction cannot push message updates as soon it's not the current.
					previousLoad?.Cancel();
				}

				if (load is not null)
				{
					// Make sure to await the end of the last projection before completing the subject!
					await load;
				}
				subject.Complete();
			}
			catch (Exception error)
			{
				subject.TryFail(error);
			}
		}
	}
}
