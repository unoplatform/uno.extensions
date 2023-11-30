using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create and manipulate <see cref="IFeed{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the data.</typeparam>
public static partial class Feed<T> // We set the T on the class to it greatly helps type inference of factory delegates
{
	/// <summary>
	/// Gets or create a custom feed from an async method.
	/// </summary>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	internal static IFeed<T> Dynamic(AsyncFunc<Option<T>> valueProvider)
		=> AttachedProperty.GetOrCreate(valueProvider, static vp => new DynamicFeed<T>(vp));

	/// <summary>
	/// Gets or create a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Message{T}"/>.
	/// </summary>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> Create(Func<CancellationToken, IAsyncEnumerable<Message<T>>> sourceProvider)
		=> AttachedProperty.GetOrCreate(sourceProvider, static sp => new CustomFeed<T>(sp));

	/// <summary>
	/// Gets or create a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Message{T}"/>.
	/// </summary>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> Create(Func<IAsyncEnumerable<Message<T>>> sourceProvider)
		=> AttachedProperty.GetOrCreate(sourceProvider, static sp => new CustomFeed<T>(_ => sp()));

	/// <summary>
	/// Gets or create a custom feed from an async method.
	/// </summary>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> Async(AsyncFunc<Option<T>> valueProvider, Signal? refresh = null)
		=> refresh is null
			? AttachedProperty.GetOrCreate(valueProvider, static vp => new AsyncFeed<T>(vp))
			: AttachedProperty.GetOrCreate(refresh, valueProvider, static (r, vp) => new AsyncFeed<T>(vp, r));

	/// <summary>
	/// Gets or create a custom feed from an async method.
	/// </summary>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> Async(AsyncFunc<T> valueProvider, Signal? refresh = null)
		=> refresh is null
			? AttachedProperty.GetOrCreate(valueProvider, static vp => new AsyncFeed<T>(vp.SomeOrNoneWhenNotNull()))
			: AttachedProperty.GetOrCreate(refresh, valueProvider, static (r, vp) => new AsyncFeed<T>(vp.SomeOrNoneWhenNotNull(), r));

	/// <summary>
	/// Gets or create a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> AsyncEnumerable(Func<IAsyncEnumerable<Option<T>>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(enumerableProvider, static ep => new AsyncEnumerableFeed<T>(ep));

	/// <summary>
	/// Gets or create a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> AsyncEnumerable(Func<CancellationToken, IAsyncEnumerable<Option<T>>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(enumerableProvider, static ep => new AsyncEnumerableFeed<T>(ep));

	/// <summary>
	/// Gets or create a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> AsyncEnumerable(Func<IAsyncEnumerable<T>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(enumerableProvider, static ep => new AsyncEnumerableFeed<T>(ep));

	/// <summary>
	/// Gets or create a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IFeed<T> AsyncEnumerable(Func<CancellationToken, IAsyncEnumerable<T>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(enumerableProvider, static ep => new AsyncEnumerableFeed<T>(ep));
}
