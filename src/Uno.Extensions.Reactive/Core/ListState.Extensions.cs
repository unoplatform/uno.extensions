using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

static partial class ListState
{
	#region Operators
	/// <summary>
	/// Adds an item into a list state
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="item">The item to add.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask AddAsync<T>(this IListState<T> state, T item, CancellationToken ct)
		=> state.UpdateValue(items => items.SomeOrDefault(ImmutableList<T>.Empty).Add(item), ct);

	/// <summary>
	/// Removes all matching items from a list state.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="match">Predicate to determine which items should be removed.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask RemoveAllAsync<T>(this IListState<T> state, Predicate<T> match, CancellationToken ct)
		=> state.UpdateValue(itemsOpt => itemsOpt.Map(items => items.RemoveAll(match)), ct);

	/// <summary>
	/// Updates all matching items from a list state.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="match">Predicate to determine which items should be removed.</param>
	/// <param name="updater">How to update items.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask UpdateAsync<T>(this IListState<T> state, Predicate<T> match, Func<T, T> updater, CancellationToken ct)
		=> state.UpdateValue(
			itemsOpt => itemsOpt.Map(items =>
			{
				var updated = items;
				foreach (var item in items)
				{
					if (match(item))
					{
						updated = items.Replace(item, updater(item));
					}
				}
				return updated;
			}),
			ct);

	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <returns>A <see cref="IDisposable"/> that can be used to remove the callback registration.</returns>
	public static IDisposable Execute<T>(this IListState<T> state, AsyncAction<IImmutableList<T>> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
		=> new StateExecute<IImmutableList<T>>(state, (list, ct) => action(list ?? ImmutableList<T>.Empty, ct), $"Execute defined in {caller} at line {line}.");
	#endregion
}
