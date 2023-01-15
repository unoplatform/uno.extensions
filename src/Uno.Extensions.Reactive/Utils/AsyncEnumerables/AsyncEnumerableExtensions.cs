using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal static class AsyncEnumerableExtensions
{
	public static async ValueTask<TSource> FirstOrDefaultAsync<TSource>(this IAsyncEnumerable<TSource> source, TSource defaultValue, CancellationToken ct)
	{
		await foreach (var value in source.WithCancellation(ct).ConfigureAwait(false))
		{
			return value;
		}

		return defaultValue;
	}

	public static Task ForEachAwaitWithCancellationAsync<TSource>(this IAsyncEnumerable<TSource> source, AsyncAction<TSource> asyncAction, ConcurrencyMode mode, CancellationToken ct)
		=> ForEachAwaitWithCancellationAsync(source, asyncAction, mode, continueOnError: false, ct);

	public static Task ForEachAwaitWithCancellationAsync<TSource>(this IAsyncEnumerable<TSource> source, AsyncAction<TSource> asyncAction, ConcurrencyMode mode, bool continueOnError, CancellationToken ct)
	{
		var manager = AsyncOperationManager.Create(mode, continueOnError);
		ct.Register(manager.Dispose);

		async Task Enumerate()
		{
			try
			{
				await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
				{
					manager.OnNext(token => asyncAction(item, token));
				}
			}
			catch (Exception error)
			{
				manager.OnError(error);
			}
			finally
			{
				manager.OnCompleted();
			}
		}

		_ = Enumerate();

		return manager.Task;
	}

	public static IAsyncEnumerable<T> Merge<T>(params IAsyncEnumerable<T>[] asyncEnumerables)
		=> new MergeAsyncEnumerable<T>(asyncEnumerables);

	public static IAsyncEnumerable<T> ToDeferredEnumerable<T>(this IAsyncEnumerable<T> source)
		=> new DeferredAsyncEnumerable<T>(source);

	public static IAsyncEnumerable<T> ToDeferredEnumerable<T>(this IAsyncEnumerable<T> source, Func<bool> deferringCondition)
		=> new ConditionalDeferredAsyncEnumerable<T>(source, deferringCondition);
}
