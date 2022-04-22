using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace Uno.Extensions.Reactive;

public static partial class State
{
	#region Sources
	// Note: Those are helpers for which the T is set by type inference on provider.
	//		 We must have only one overload per method.

	/// <summary>
	/// Creates a custom feed from a raw <see cref="IAsyncEnumerable{T}"/> sequence of <see cref="Uno.Extensions.Reactive.Message{T}"/>.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting feed.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="sourceProvider">The provider of the message enumerable sequence.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<TValue> Create<TOwner, TValue>(TOwner owner, Func<CancellationToken, IAsyncEnumerable<Message<TValue>>> sourceProvider)
		where TOwner : class
		=> State<TValue>.Create(owner, sourceProvider);

	/// <summary>
	/// Creates a custom feed from an async method.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting feed.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="valueProvider">The async method to use to load the value of the resulting feed.</param>
	/// <param name="refresh">A refresh trigger to reload the <paramref name="valueProvider"/>.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<TValue> Async<TOwner, TValue>(TOwner owner, AsyncFunc<TValue> valueProvider, Signal? refresh = null)
		where TOwner : class
		=> State<TValue>.Async(owner, valueProvider, refresh);

	/// <summary>
	/// Creates a custom feed from an async enumerable sequence of value.
	/// </summary>
	/// <typeparam name="TOwner">Type of the owner of the state.</typeparam>
	/// <typeparam name="TValue">The type of the value of the resulting feed.</typeparam>
	/// <param name="owner">The owner of the state.</param>
	/// <param name="enumerableProvider">The async enumerable sequence of value of the resulting feed.</param>
	/// <returns>A feed that encapsulate the source.</returns>
	public static IState<TValue> AsyncEnumerable<TOwner, TValue>(TOwner owner, Func<IAsyncEnumerable<TValue>> enumerableProvider)
		where TOwner : class
		=> State<TValue>.AsyncEnumerable(owner, enumerableProvider);
	#endregion
}
