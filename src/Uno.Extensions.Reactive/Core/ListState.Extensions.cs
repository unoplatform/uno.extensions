using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

static partial class ListState
{
	/// <summary>
	/// Updates the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="updater">The update method to apply to the current value.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask Update<T>(this IListState<T> state, Func<IImmutableList<T>, IImmutableList<T>> updater, CancellationToken ct)
		=> state.UpdateMessage(
			m =>
			{
				var updatedValue = updater(m.CurrentData.SomeOrDefault() ?? ImmutableList<T>.Empty);
				var updatedData = updatedValue is null or {Count: 0} ? Option<IImmutableList<T>>.None() : Option.Some(updatedValue);

				m.Data(updatedData);
			},
			ct);

	/// <summary>
	/// Updates the value of a list state
	/// </summary>
	/// <typeparam name="T">Type of the items of the list state.</typeparam>
	/// <param name="state">The list state to update.</param>
	/// <param name="updater">The update method to apply to the current list.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask UpdateData<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, Option<IImmutableList<T>>> updater, CancellationToken ct)
		=> state.UpdateMessage(m => m.Data(updater(m.CurrentData)), ct);

	/// <summary>
	/// Updates the value of a list state
	/// </summary>
	/// <typeparam name="T">Type of the items of the list state.</typeparam>
	/// <param name="state">The list state to update.</param>
	/// <param name="updater">The update method to apply to the current list.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask UpdateData<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, IImmutableList<T>> updater, CancellationToken ct)
		=> state.UpdateMessage(m => m.Data(updater(m.CurrentData)), ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateData instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateData")]
#endif
	public static ValueTask UpdateValue<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, Option<IImmutableList<T>>> updater, CancellationToken ct)
		=> UpdateData(state, updater, ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateData instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateData")]
#endif
	public static ValueTask UpdateValue<T>(this IListState<T> state, Func<Option<IImmutableList<T>>, IImmutableList<T>> updater, CancellationToken ct)
		=> UpdateData(state, updater, ct);


	#region Operators
	/// <summary>
	/// Adds an item into a list state
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="item">The item to add.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask InsertAsync<T>(this IListState<T> state, T item, CancellationToken ct)
		=> state.UpdateData(items => items.SomeOrDefault(ImmutableList<T>.Empty).Insert(0, item), ct);

	/// <summary>
	/// Adds an item into a list state
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="item">The item to add.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask AddAsync<T>(this IListState<T> state, T item, CancellationToken ct)
		=> state.UpdateData(items => items.SomeOrDefault(ImmutableList<T>.Empty).Add(item), ct);

	/// <summary>
	/// Removes all matching items from a list state.
	/// </summary>
	/// <typeparam name="T">The type of the items in the list.</typeparam>
	/// <param name="state">The list state onto which the item should be added.</param>
	/// <param name="match">Predicate to determine which items should be removed.</param>
	/// <param name="ct">A token to abort the async add operation.</param>
	/// <returns></returns>
	public static ValueTask RemoveAllAsync<T>(this IListState<T> state, Predicate<T> match, CancellationToken ct)
		=> state.UpdateData(itemsOpt => itemsOpt.Map(items => items.RemoveAll(match)), ct);

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
		=> state.UpdateData(
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
	/// [DEPRECATED] Use .ForEachAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static IDisposable Execute<T>(this IListState<T> state, AsyncAction<IImmutableList<T>> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
		=> ForEachAsync(state, action, caller, line);


	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <returns>A <see cref="IDisposable"/> that can be used to remove the callback registration.</returns>
	public static IDisposable ForEachAsync<T>(this IListState<T> state, AsyncAction<IImmutableList<T>> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
		=> new StateForEach<IImmutableList<T>>(state, (list, ct) => action(list ?? ImmutableList<T>.Empty, ct), $"ForEachAsync defined in {caller} at line {line}.");
	#endregion
}
