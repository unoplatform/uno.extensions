using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

[Flags]
public enum FeedAsyncValue
{
	Default = 0,

	AllowTransient = 1,

	AllowError = 2,

	All = AllowTransient | AllowError,
}

public static partial class FeedExtensions
{
	public static ValueTaskAwaiter<Option<T>> GetAwaiter<T>(this IFeed<T> feed)
		=> feed.Value(SourceContext.Current.Token).GetAwaiter();

	public static ValueTask<Option<T>> Value<T>(this IFeed<T> feed, CancellationToken ct)
		=> feed.Values(FeedAsyncValue.Default, ct).FirstOrDefaultAsync(Option<T>.Undefined(), ct);

	public static async IAsyncEnumerable<Option<T>> Values<T>(this IFeed<T> feed, FeedAsyncValue kind = FeedAsyncValue.AllowError, [EnumeratorCancellation] CancellationToken ct = default)
	{
		await foreach (var message in SourceContext.Current.GetOrCreateSource(feed).WithCancellation(ct).ConfigureAwait(false))
		{
			var current = message.Current;

			if (current.IsTransient && !kind.HasFlag(FeedAsyncValue.AllowTransient))
			{
				continue;
			}

			if (current.Error is { } error && !kind.HasFlag(FeedAsyncValue.AllowError))
			{
				ExceptionDispatchInfo.Capture(error).Throw();
			}

			yield return current.Data;
		}
	}

	public static ValueTask<Message<T>> Message<T>(this IFeed<T> feed)
		=> feed.Messages().FirstAsync(SourceContext.Current.Token);

	public static ValueTask<Message<T>> Message<T>(this IFeed<T> feed, CancellationToken ct)
		=> feed.Messages().FirstAsync(ct);

	public static IAsyncEnumerable<Message<T>> Messages<T>(this IFeed<T> feed)
		=> SourceContext.Current.GetOrCreateSource(feed);
}
