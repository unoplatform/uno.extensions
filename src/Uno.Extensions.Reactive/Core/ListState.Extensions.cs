using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Provides a set of static methods to create and manipulate <see cref="IListState{T}"/>.
/// </summary>
public static class ListState
{
	#region Operators
	/// <summary>
	/// Adds an items into the state
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="item">The item to add.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask AddAsync<T>(this IListState<T> state, T item, CancellationToken ct)
		=> state.UpdateValue(items => items.SomeOrDefault(ImmutableList<T>.Empty).Add(item), ct);
	#endregion
}
