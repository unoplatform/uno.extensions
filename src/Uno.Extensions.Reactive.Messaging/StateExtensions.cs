using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Messaging;

/// <summary>
/// Set of extensions to update an <see cref="IState{T}"/> from messaging structures.
/// </summary>
public static class StateExtensions
{
	/// <summary>
	/// Updates a state using an <see cref="EntityMessage{TEntity}"/>.
	/// </summary>
	/// <typeparam name="TEntity">Type of the value of teh state.</typeparam>
	/// <typeparam name="TKey">Type of the identifier that uniquely identifies a <typeparamref name="TEntity"/>.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="message">The update message to apply.</param>
	/// <param name="keySelector">A selector to get a unique identifier of a <typeparamref name="TEntity"/>.</param>
	/// <param name="ct">A cancellation token to abort the async operation</param>
	/// <returns>An async operation of the update.</returns>
	public static async ValueTask Update<TEntity, TKey>(this IState<TEntity> state, EntityMessage<TEntity> message, Func<TEntity, TKey> keySelector, CancellationToken ct)
	{
		switch (message.Change)
		{
			case EntityChange.Updated:
				var updatedEntityKey = keySelector(message.Value);
				await state.UpdateDataAsync(current => current.IsSome(out var entity) && AreKeyEquals(updatedEntityKey, keySelector(entity)) ? message.Value : current, ct);
				break;

			case EntityChange.Deleted:
				var removedEntityKey = keySelector(message.Value);
				await state.UpdateDataAsync(current => current.IsSome(out var entity) && AreKeyEquals(removedEntityKey, keySelector(entity)) ? Option<TEntity>.None() : current, ct);
				break;
		}

		static bool AreKeyEquals(TKey left, TKey right)
			=> left?.Equals(right) ?? right is null;
	}

	/// <summary>
	/// Updates a state using an <see cref="EntityMessage{TEntity}"/>.
	/// </summary>
	/// <typeparam name="TEntity">Type of the value of teh state.</typeparam>
	/// <typeparam name="TKey">Type of the identifier that uniquely identifies a <typeparamref name="TEntity"/>.</typeparam>
	/// <param name="listState">The state to update.</param>
	/// <param name="message">The update message to apply.</param>
	/// <param name="keySelector">A selector to get a unique identifier of a <typeparamref name="TEntity"/>.</param>
	/// <param name="ct">A cancellation token to abort the async operation</param>
	/// <returns>An async operation of the update.</returns>
	public static async ValueTask Update<TEntity, TKey>(this IListState<TEntity> listState, EntityMessage<TEntity> message, Func<TEntity, TKey> keySelector, CancellationToken ct)
	{
		switch (message.Change)
		{
			case EntityChange.Created:
				await listState.AddAsync(message.Value, ct);
				break;

			case EntityChange.Deleted:
				var removedItemKey = keySelector(message.Value);
				await listState.RemoveAllAsync(item => AreKeyEquals(removedItemKey, keySelector(item)), ct);
				break;

			case EntityChange.Updated:
				var updatedItemKey = keySelector(message.Value);
				await listState.UpdateAllAsync(item => AreKeyEquals(updatedItemKey, keySelector(item)), _ => message.Value, ct);
				break;
		}

		static bool AreKeyEquals(TKey left, TKey right)
			=> left?.Equals(right) ?? right is null;
	}
}
