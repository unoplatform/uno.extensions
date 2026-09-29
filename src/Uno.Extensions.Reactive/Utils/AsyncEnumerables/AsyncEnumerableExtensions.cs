using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal static class AsyncEnumerableExtensions
{
	public static async Task ForEachAsync<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource> action, CancellationToken ct = default)
	{
		await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
		{
			action(item);
		}
	}

	public static async Task ForEachAsync<TSource>(this IAsyncEnumerable<TSource> source, Action<TSource, int> action, CancellationToken ct = default)
	{
		var index = 0;
		await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
		{
			action(item, index++);
		}
	}

	public static async Task ForEachAwaitAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, Task> asyncAction, CancellationToken ct = default)
	{
		await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
		{
			await asyncAction(item).ConfigureAwait(false);
		}
	}

	public static async Task ForEachAwaitWithCancellationAsync<TSource>(this IAsyncEnumerable<TSource> source, Func<TSource, CancellationToken, Task> asyncAction, CancellationToken ct)
	{
		await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
		{
			await asyncAction(item, ct).ConfigureAwait(false);
		}
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
