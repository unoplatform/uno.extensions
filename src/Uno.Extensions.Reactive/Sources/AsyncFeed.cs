using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Reactive.Core.FeedHelper;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class AsyncFeed<T> : IFeed<T>, IRefreshableSource
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
		var versions = new AsyncEnumerableSubject<RefreshToken>(ReplayMode.EnabledForFirstEnumeratorOnly);
		var current = RefreshToken.Initial(this, context);

		// Request initial load (without refresh)
		versions.SetNext(current);

		// Then subscribe to refresh sources
		var localRefreshTask = _refresh?.GetSource(context, ct).ForEachAsync(BeginRefresh, ct);
		var contextRefreshTask = context.Requests<RefreshRequest>().ForEachAsync(Refresh, ct);

		localRefreshTask?.ContinueWith(TryComplete, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);
		contextRefreshTask.ContinueWith(TryComplete, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);

		void Refresh(RefreshRequest request)
		{
			var refreshedVersion = RefreshToken.InterlockedIncrement(ref current);

			request.Register(refreshedVersion);
			versions.SetNext(refreshedVersion);
		}
		void BeginRefresh(Unit _)
		{
			var refreshedVersion = RefreshToken.InterlockedIncrement(ref current);

			versions.SetNext(refreshedVersion);
		}

		void TryComplete(Task _)
		{
			if (localRefreshTask is not { IsCompleted: false } && contextRefreshTask is { IsCompleted: true })
			{
				versions.TryComplete();
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
				var version = versions.GetAsyncEnumerator(ct);
				while (await version.MoveNextAsync(ct).ConfigureAwait(false))
				{
					var previousLoad = loadToken;
					// Capture the version so if while loop exit we still have the right value.
					// We also make sure to convert it only once in SourceVersionCollection so we keep the same instance in case of multiple set by the InvokeAsync
					var loadVersion = (RefreshTokenCollection)version.Current;
					loadToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
					load = InvokeAsync(
						message,
						null,
						_dataProvider,
						b =>
						{
							if (loadVersion.Versions.First() is { Version: > 0 })
							{
								b.Set(MessageAxis.Refresh, loadVersion);
							}
						},
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
