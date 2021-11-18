using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Impl.Operators;

namespace Uno.Extensions.Reactive;

public static partial class Feed
{
	#region Sources
	// Note: Those are helpers for which the T is set by type inference on provider.
	//		 We must have only one overload per method.

	public static IFeed<T> Create<T>(Func<CancellationToken, IAsyncEnumerable<Message<T>>> sourceProvider)
		=> Feed<T>.Create(sourceProvider);

	public static IFeed<T> Async<T>(FuncAsync<T> valueProvider, Signal? refresh = null)
		=> Feed<T>.Async(valueProvider, refresh);

	public static IFeed<T> AsyncEnumerable<T>(Func<IAsyncEnumerable<Option<T>>> enumerableProvider)
		=> Feed<T>.AsyncEnumerable(enumerableProvider);
	#endregion

	#region Operators
	// Note: The operators are only dealing with values.
	//		 To deal with Message<T> or Option<T>, we will request to user to enumerate themselves the source

	public static IFeed<TSource> Where<TSource>(
		this IFeed<TSource> source,
		Predicate<TSource?> predicate)
		=> AttachedProperty.GetOrCreate(source, predicate, (src, p) => new WhereFeed<TSource>(src, p));

	public static IFeed<TResult> Select<TSource, TResult>(
		this IFeed<TSource> source,
		Func<TSource?, TResult?> selector)
		=> AttachedProperty.GetOrCreate(source, selector, (src, s) => new SelectFeed<TSource, TResult>(src, s));

	public static IFeed<TResult> SelectAsync<TSource, TResult>(
		this IFeed<TSource> source,
		FuncAsync<TSource?, TResult?> selector)
		=> AttachedProperty.GetOrCreate(source, selector, (src, s) => new SelectAsyncFeed<TSource, TResult>(src, s));
	#endregion
}
