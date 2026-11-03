using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// Declared in the root namespace (not .Utils) so that, for every caller in Uno.Extensions.Reactive.*,
// these bind ahead of the [Obsolete] System.Linq.Async 7 versions imported by `using System.Linq;`.
namespace Uno.Extensions.Reactive;

internal static class AsyncEnumerableForEachExtensions
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
}
