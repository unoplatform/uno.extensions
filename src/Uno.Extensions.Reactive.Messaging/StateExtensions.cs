using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Messaging;

/// <summary>
/// Set of extensions to update an <see cref="IState{T}"/> from messaging structures.
/// </summary>
public static class StateExtensions
{
	/// <summary>
	/// Updates a state using an <see cref="EntityMessage{TEntity}"/>.
	/// </summary>
	/// <typeparam name="TEntity">Type of the value of the state.</typeparam>
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
	/// <typeparam name="TEntity">Type of the value of the state.</typeparam>
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

	/// <summary>
	/// Request to refresh the source underlying the given state.
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to refresh.</param>
	/// <returns>A boolean indicating if the source is refreshing or not.</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static bool RequestRefresh<T>(this IState<T> state)
		=> !state.Requests.RequestRefresh().IsEmpty;

	/// <summary>
	/// Request to refresh the source underlying the given list state.
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="listState">The list state to refresh.</param>
	/// <returns>A boolean indicating if the source is refreshing or not.</returns>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static bool RequestRefresh<T>(this IListState<T> listState)
		=> !listState.Requests.RequestRefresh().IsEmpty;

	/// <summary>
	/// Request to refresh the source underlying the given state and wait for the refresh to complete (i.e. wait for the state to publish a message reflecting the result of the refresh).
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to refresh.</param>
	/// <param name="ct">An cancellation to abort the asynchronous operation, cf. remarks for details.</param>
	/// <returns>An asynchronous boolean indicating if the source has been refreshed or not.</returns>
	/// <remarks>
	/// Cancelling the <paramref name="ct"/> will only cancel the wait for the refreshed message on state, but it won't cancel the refresh itself.
	/// Once refreshed has been requested, it cannot be cancelled.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static async ValueTask<bool> TryRefreshAsync<T>(this IState<T> state, CancellationToken ct = default)
	{
		var req = state.Requests.RequestRefresh();
		if (req.IsEmpty)
		{
			return false;
		}

		var awaiter = new TokenSetAwaiter<RefreshToken>();
		var refreshed = awaiter.WaitFor(req, ct);
		var messageListener = state
			.GetSource(state.Context, ct)
			.Where(msg => !msg.Current.IsTransient)
			.ForEachAsync(msg => awaiter.Received(msg.Current.Get(MessageAxis.Refresh)), ct);

		return await Task.WhenAny(refreshed, messageListener) == refreshed;
	}

	/// <summary>
	/// Request to refresh the source underlying the given state and wait for the refresh to complete (i.e. wait for the state to publish a message reflecting the result of the refresh).
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="listState">The list state to refresh.</param>
	/// <param name="ct"> A cancellation to abort the asynchronous operation, cf. remarks for details.</param>
	/// <returns>An asynchronous boolean indicating if the source has been refreshed or not.</returns>
	/// <remarks>
	/// Cancelling the <paramref name="ct"/> will only cancel the wait for the refreshed message on state, but it won't cancel the refresh itself.
	/// Once refreshed has been requested, it cannot be canceled.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public static async ValueTask<bool> TryRefreshAsync<T>(this IListState<T> listState, CancellationToken ct = default)
	{
		var req = listState.Requests.RequestRefresh();
		if (req.IsEmpty)
		{
			return false;
		}

		var awaiter = new TokenSetAwaiter<RefreshToken>();
		var refreshed = awaiter.WaitFor(req, ct);
		var messageListener = listState
			.GetSource(listState.Context, ct)
			.Where(msg => !msg.Current.IsTransient)
			.ForEachAsync(msg => awaiter.Received(msg.Current.Get(MessageAxis.Refresh)), ct);

		return await Task.WhenAny(refreshed, messageListener) == refreshed;
	}
}
