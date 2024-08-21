using CommunityToolkit.Mvvm.Messaging;

namespace Uno.Extensions.Reactive.Messaging;

/// <summary>
/// Set of extensions to update an <see cref="IListState{T}"/> from messaging structures.
/// </summary>
public static class ListStateExtensions
{
	/// <summary>
	/// Listen for <see cref="EntityMessage{TEntity}"/> on the given <paramref name="messenger"/> and updates the <paramref name="listState"/> accordingly.
	/// </summary>
	/// <typeparam name="TEntity">Type of the value of the state.</typeparam>
	/// <typeparam name="TKey">Type of the identifier that uniquely identifies a <typeparamref name="TEntity"/>.</typeparam>
	/// <param name="listState">The list state to update.</param>
	/// <param name="messenger">The messenger to listen for <see cref="EntityMessage{TEntity}"/></param>
	/// <param name="keySelector">A selector to get a unique identifier of a <typeparamref name="TEntity"/>.</param>
	/// <param name="disposable"> A disposable that can be used to unbind the state from the messenger.</param>
	/// <returns>An <see cref="IListState{TEntity}"/> that can be used to chain other operations.</returns>
	public static IListState<TEntity> Observe<TEntity, TKey>(this IListState<TEntity> listState, IMessenger messenger, Func<TEntity, TKey> keySelector, out IDisposable disposable)
	{
		disposable = messenger.Observe(listState, keySelector);
		return listState;
	}

	/// <summary>
	/// Listen for <see cref="EntityMessage{TEntity}"/> on the given <paramref name="messenger"/> and updates the <paramref name="listState"/> accordingly.
	/// </summary>
	/// <typeparam name="TEntity">Type of the value of the state.</typeparam>
	/// <typeparam name="TKey">Type of the identifier that uniquely identifies a <typeparamref name="TEntity"/>.</typeparam>
	/// <param name="listState">The list state to update.</param>
	/// <param name="messenger">The messenger to listen for <see cref="EntityMessage{TEntity}"/></param>
	/// <param name="keySelector">A selector to get a unique identifier of a <typeparamref name="TEntity"/>.</param>
	/// <returns>An <see cref="IListState{TEntity}"/> that can be used to chain other operations.</returns>
	public static IListState<TEntity> Observe<TEntity, TKey>(this IListState<TEntity> listState, IMessenger messenger, Func<TEntity, TKey> keySelector)
	{
		_ = messenger.Observe(listState, keySelector);
		return listState;
	}
}
