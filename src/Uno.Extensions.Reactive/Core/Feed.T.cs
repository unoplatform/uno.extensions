using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Sources;
using Uno.Reactive.Sources;

namespace Uno.Extensions.Reactive;

public static class Feed<T> // We set the T on the class to it greatly helps type inference of factory delegates
{
	public static IFeed<T> Create(Func<CancellationToken, IAsyncEnumerable<Message<T>>> sourceProvider)
		=> AttachedProperty.GetOrCreate(sourceProvider, sp => new CustomFeed<T>(sp));

	public static IFeed<T> Create(Func<IAsyncEnumerable<Message<T>>> sourceProvider)
		=> AttachedProperty.GetOrCreate(sourceProvider, sp => new CustomFeed<T>(_ => sp()));


	public static IFeed<T> Async(FuncAsync<Option<T>> valueProvider, Signal? refresh = null)
		=> refresh is null
			? AttachedProperty.GetOrCreate(valueProvider, vp => new AsyncFeed<T>(vp))
			: AttachedProperty.GetOrCreate(refresh, valueProvider, (r, vp) => new AsyncFeed<T>(vp, r));

	public static IFeed<T> Async(FuncAsync<T> valueProvider, Signal? refresh = null)
		=> refresh is null
			? AttachedProperty.GetOrCreate(valueProvider, vp => new AsyncFeed<T>(vp))
			: AttachedProperty.GetOrCreate(refresh, valueProvider, (r, vp) => new AsyncFeed<T>(vp, r));

	public static IFeed<T> AsyncEnumerable(Func<IAsyncEnumerable<Option<T>>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(enumerableProvider, ep => new AsyncEnumerableFeed<T>(ep));

	public static IFeed<T> AsyncEnumerable(Func<IAsyncEnumerable<T>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(enumerableProvider, ep => new AsyncEnumerableFeed<T>(ep));
}
