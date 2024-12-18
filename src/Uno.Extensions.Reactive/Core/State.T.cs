using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create and manipulate <see cref="IState{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the data.</typeparam>
public static partial class State<T>
{
	/// <summary>
	/// Creates a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Message{T}"/>.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> Create<TOwner>(TOwner owner, Func<CancellationToken, IAsyncEnumerable<Message<T>>> sourceProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, sourceProvider, static (o, sp) => S(o, new CustomFeed<T>(sp)));

	/// <summary>
	/// Creates a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Message{T}"/>.
	/// </summary>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static IState<T> Create(Func<CancellationToken, IAsyncEnumerable<Message<T>>> sourceProvider)
		=> AttachedProperty.GetOrCreate(Validate(sourceProvider), static sp => S(sp, new CustomFeed<T>(sp)));

	/// <summary>
	/// Creates a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Message{T}"/>.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> Create<TOwner>(TOwner owner, Func<IAsyncEnumerable<Message<T>>> sourceProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, sourceProvider, static (o, sp) => S(o, new CustomFeed<T>(_ => sp())));

	/// <summary>
	/// Creates a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Message{T}"/>.
	/// </summary>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static IState<T> Create(Func<IAsyncEnumerable<Message<T>>> sourceProvider)
		=> AttachedProperty.GetOrCreate(Validate(sourceProvider), static sp => S(sp, new CustomFeed<T>(_ => sp())));

	/// <summary>
	/// Gets or creates an empty state.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="name">The caller member where the state is being declared in code and which is used in the key to uniquely identify the state.</param>
	/// <param name="line">The line where the state is being declared in code and which is used in the key to uniquely identify the state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> Empty<TOwner>(TOwner owner, [CallerMemberName] string? name = null, [CallerLineNumber] int line = -1)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(
			owner,
			(
				name ?? throw new InvalidOperationException("The name of the state must not be null"),
				line <0 ? throw new InvalidOperationException("The provided line number is invalid.") : line
			),
			static (o, _) => SourceContext.GetOrCreate(o).CreateState(Option<T>.None()));

	/// <summary>
	/// Gets or creates a state from a static initial value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The provider of the initial value of the state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> Value<TOwner>(TOwner owner, Func<T> valueProvider)
		where TOwner : class
		// Note: We force the usage of delegate so 2 properties which are doing State.Value(this, () => 42) will effectively have 2 distinct states.
		=> AttachedProperty.GetOrCreate(owner: owner,
										key: valueProvider,
										factory: static (o, v) => SourceContext.GetOrCreate(o).CreateState(Option.SomeOrNone(v())));

	/// <summary>
	/// Gets or creates a state from a static initial value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The provider of the initial value of the state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> Value<TOwner>(TOwner owner, Func<Option<T>> valueProvider)
		where TOwner : class
		// Note: We force the usage of delegate so 2 properties which are doing State.Value(this, () => 42) will effectively have 2 distinct states.
		=> AttachedProperty.GetOrCreate(owner: owner,
										key: valueProvider,
										factory: static (o, v) => SourceContext.GetOrCreate(o).CreateState(v()));

	/// <summary>
	/// Gets or creates a state from an async method.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> Async<TOwner>(TOwner owner, AsyncFunc<Option<T>> valueProvider, Signal? refresh = null)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner: owner,
										key: (valueProvider, refresh),
										factory: static (o, args) => S(o, new AsyncFeed<T>(args.valueProvider, args.refresh)));

	/// <summary>
	/// Gets or creates a state from an async method.
	/// </summary>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static IState<T> Async(AsyncFunc<Option<T>> valueProvider, Signal? refresh = null)
		=> refresh is null
			? AttachedProperty.GetOrCreate(Validate(valueProvider), vp => S(vp, new AsyncFeed<T>(vp)))
			: AttachedProperty.GetOrCreate(refresh, Validate(valueProvider), static (r, vp) => S(vp, new AsyncFeed<T>(vp, r)));

	/// <summary>
	/// Gets or creates a state from an async method.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> Async<TOwner>(TOwner owner, AsyncFunc<T> valueProvider, Signal? refresh = null)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner: owner,
										key: (valueProvider, refresh),
										factory: static (o, args) => S(o, new AsyncFeed<T>(args.valueProvider.SomeOrNoneWhenNotNull(), args.refresh)));

	/// <summary>
	/// Gets or creates a state from an async method.
	/// </summary>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static IState<T> Async(AsyncFunc<T> valueProvider, Signal? refresh = null)
		=> refresh is null
			? AttachedProperty.GetOrCreate(Validate(valueProvider), static vp => S(vp, new AsyncFeed<T>(vp.SomeOrNoneWhenNotNull())))
			: AttachedProperty.GetOrCreate(refresh, Validate(valueProvider), static (r, vp) => S(vp, new AsyncFeed<T>(vp.SomeOrNoneWhenNotNull(), r)));

	/// <summary>
	/// Gets or creates a state from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> AsyncEnumerable<TOwner>(TOwner owner, Func<IAsyncEnumerable<Option<T>>> enumerableProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, enumerableProvider, static (o, ep) => S(o, new AsyncEnumerableFeed<T>(ep)));

	/// <summary>
	/// Gets or creates a state from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> AsyncEnumerable<TOwner>(TOwner owner, Func<CancellationToken, IAsyncEnumerable<Option<T>>> enumerableProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, enumerableProvider, static (o, ep) => S(o, new AsyncEnumerableFeed<T>(ep)));

	/// <summary>
	/// Gets or creates a state from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static IState<T> AsyncEnumerable(Func<IAsyncEnumerable<Option<T>>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(Validate(enumerableProvider), static ep => S(ep, new AsyncEnumerableFeed<T>(ep)));

	/// <summary>
	/// Gets or creates a state from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> AsyncEnumerable<TOwner>(TOwner owner, Func<IAsyncEnumerable<T>> enumerableProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, enumerableProvider, static (o, ep) => S(o, new AsyncEnumerableFeed<T>(ep)));

	/// <summary>
	/// Gets or creates a state from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> AsyncEnumerable<TOwner>(TOwner owner, Func<CancellationToken, IAsyncEnumerable<T>> enumerableProvider)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, enumerableProvider, static (o, ep) => S(o, new AsyncEnumerableFeed<T>(ep)));

	/// <summary>
	/// Gets or creates a state from an async enumerable sequence of value.
	/// </summary>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static IState<T> AsyncEnumerable(Func<IAsyncEnumerable<T>> enumerableProvider)
		=> AttachedProperty.GetOrCreate(Validate(enumerableProvider), static ep => S(ep, new AsyncEnumerableFeed<T>(ep)));

	/// <summary>
	/// Gets or creates a state from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="feed">The source feed of the resulting state.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<T> FromFeed<TOwner>(TOwner owner, IFeed<T> feed)
		where TOwner : class
		=> AttachedProperty.GetOrCreate(owner, feed, static (o, f) => S(o, f));

	private static TKey Validate<TKey>(TKey key, [CallerMemberName] string? caller = null)
		where TKey : Delegate
	{
		// TODO: We should make sure to **not** allow method group on an **external** object.
		//		 This would allow creation of State on external object (like a Service) which would be weird.
		//if (key.Target is not ISourceContextAware)
		if (key.Target is null)
		{
			throw new InvalidOperationException($"The delegate provided in the Command.{caller} must not be a static method.");
		}

		return key;
	}

	internal static IState<T> S<TOwner>(TOwner owner, IFeed<T> feed)
		where TOwner : class
		// We make sure to use the SourceContext to create the State, so it will be disposed with the context.
		=> SourceContext.GetOrCreate(owner).GetOrCreateState(feed);
}
