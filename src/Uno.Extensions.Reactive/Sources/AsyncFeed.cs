using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Core.HotReload;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Reactive.Core.FeedHelper;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class AsyncFeed<T> : IFeed<T>
{
	private readonly ISignal? _refresh;
	private readonly AsyncFunc<Option<T>> _dataProvider;
	private readonly Type? _sourceType;

	public AsyncFeed(AsyncFunc<T?> dataProvider, ISignal? refresh = null, Type? sourceType = null)
	{
		_sourceType = sourceType ?? dataProvider.Method.DeclaringType;
		_dataProvider = dataProvider.SomeOrNone();
		_refresh = refresh;
	}

	public AsyncFeed(AsyncFunc<Option<T>> dataProvider, ISignal? refresh = null, Type? sourceType = null)
	{
		_sourceType = sourceType ?? dataProvider.Method.DeclaringType;
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
		Action? unsubscribeHotReload = null;
		context.Requests<RefreshRequest>(Refresh, ct);
		context.Requests<EndRequest>(_ =>
			{
				contextRefreshEnded = true;
				TryComplete(null);
			},
			ct);

		localRefreshTask?.ContinueWith(TryComplete, TaskContinuationOptions.ExecuteSynchronously);

		if (FeedConfiguration.EffectiveHotReload.HasFlag(HotReloadSupport.AsyncFeed) && _sourceType is not null)
		{
			// If the feed was constructed AFTER a previous hot-reload, _sourceType may itself be an EnC shadow type
			// (e.g. MyModel#3+<>c). Resolve it back to the original so subsequent HR notifications, which we also
			// resolve to their original, compare equal.
#pragma warning disable IL2026 // Underlying API uses reflection on a per-assembly attribute that cannot be statically known.
			var rootSourceType = GetOriginalRootType(_sourceType);

			void OnApplicationUpdated(Type[] types)
			{
				// Each updated type can be either the user type itself or a compiler-generated
				// nested type (lambda <>c, DisplayClass, async state-machine). Walk up to the
				// outermost type, then resolve the EnC "shadow" type back to its original via
				// MetadataUpdateOriginalTypeAttribute, so we match the captured root source type.
				if (types.Any(t => GetOriginalRootType(t) == rootSourceType))
				{
					var refreshedVersion = RefreshToken.InterlockedIncrement(ref current);
					// Use TrySetNext so a notification arriving in a tiny race window between completion
					// and unsubscription does not throw on the hot-reload callback thread.
					loadRequests.TrySetNext(refreshedVersion);
				}
			}
#pragma warning restore IL2026

			HotReloadService.ApplicationUpdated += OnApplicationUpdated;
			unsubscribeHotReload = () => HotReloadService.ApplicationUpdated -= OnApplicationUpdated;
			ct.Register(() => Interlocked.Exchange(ref unsubscribeHotReload, null)?.Invoke());
		}

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
				// Unsubscribe before completing so a HR notification cannot push to a completed subject.
				Interlocked.Exchange(ref unsubscribeHotReload, null)?.Invoke();
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
					await load.ConfigureAwait(false);
				}
				subject.Complete();
			}
			catch (Exception error)
			{
				subject.TryFail(error);
			}
		}
	}

	// Walks DeclaringType up to the outermost type, then resolves an EnC "shadow" type
	// (e.g. MyModel#3, produced when hot-reload rewrites a type) back to its original via
	// MetadataUpdateOriginalTypeAttribute. Both sides of the HR comparison go through this
	// so a feed constructed after a prior hot-reload (whose lambda lives on a shadow type)
	// still matches metadata updates that point at later shadow generations of the same type.
	[RequiresUnreferencedCode("`MetadataUpdateOriginalTypeAttribute` may be a per-assembly type, so it cannot be statically known.")]
	private static Type GetOriginalRootType(Type type)
	{
		while (type.DeclaringType is { } declaring)
		{
			type = declaring;
		}
		return HotReloadService.GetOriginalType(type) ?? type;
	}
}
