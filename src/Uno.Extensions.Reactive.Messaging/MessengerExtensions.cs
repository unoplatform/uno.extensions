using System;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Messaging;

/// <summary>
/// Set of extensions to bind an <see cref="IMessenger"/> with an <see cref="IState{T}"/>.
/// </summary>
public static class MessengerExtensions
{
	/// <summary>
	/// Listen for <see cref="EntityMessage{TEntity}"/> on the given <paramref name="messenger"/> and updates the <paramref name="state"/> accordingly.
	/// </summary>
	/// <typeparam name="TEntity">Type of the value of the state.</typeparam>
	/// <typeparam name="TKey">Type of the identifier that uniquely identifies a <typeparamref name="TEntity"/>.</typeparam>
	/// <param name="messenger">The messenger to listen for <see cref="EntityMessage{TEntity}"/></param>
	/// <param name="state">The state to update.</param>
	/// <param name="keySelector">A selector to get a unique identifier of a <typeparamref name="TEntity"/>.</param>
	/// <returns>A disposable that can be used to unbind the state from the messenger.</returns>
	public static IDisposable Observe<TEntity, TKey>(this IMessenger messenger, IState<TEntity> state, Func<TEntity, TKey> keySelector)
		=> AttachedProperty.GetOrCreate(state, keySelector, messenger, (s, ks, msg) => new Recipient<IState<TEntity>, TEntity, TKey>(s, msg, ks, StateExtensions.Update));

	/// <summary>
	/// Listen for <see cref="EntityMessage{TEntity}"/> on the given <paramref name="messenger"/> and updates the <paramref name="listState"/> accordingly.
	/// </summary>
	/// <typeparam name="TEntity">Type of the value of the state.</typeparam>
	/// <typeparam name="TKey">Type of the identifier that uniquely identifies a <typeparamref name="TEntity"/>.</typeparam>
	/// <param name="messenger">The messenger to listen for <see cref="EntityMessage{TEntity}"/></param>
	/// <param name="listState">The list state to update.</param>
	/// <param name="keySelector">A selector to get a unique identifier of a <typeparamref name="TEntity"/>.</param>
	/// <returns>A disposable that can be used to unbind the state from the messenger.</returns>
	public static IDisposable Observe<TEntity, TKey>(this IMessenger messenger, IListState<TEntity> listState, Func<TEntity, TKey> keySelector)
		=> AttachedProperty.GetOrCreate(listState, keySelector, messenger, (s, ks, msg) => new Recipient<IListState<TEntity>, TEntity, TKey>(s, msg, ks, StateExtensions.Update));

	/// <summary>
	/// Listen for <see cref="EntityMessage{TEntity}"/> on the given <paramref name="messenger"/>, matches it with another value and updates the <paramref name="state"/> accordingly.
	/// </summary>
	/// <typeparam name="TOther">Type of the other value to validate.</typeparam>
	/// <typeparam name="TEntity">Type of the value of the state.</typeparam>
	/// <typeparam name="TKey">Type of the identifier that uniquely identifies a <typeparamref name="TEntity"/>.</typeparam>
	/// <param name="messenger">The messenger to listen for <see cref="EntityMessage{TEntity}"/></param>
	/// <param name="state">The state to update.</param>
	/// <param name="other">The other value to validate with the updated entity using <paramref name="predicate"/>.</param>
	/// <param name="predicate">A predicted used with the updated entity to confirm that the change should by applied to the <paramref name="state"/>.</param>
	/// <param name="keySelector">A selector to get a unique identifier of a <typeparamref name="TEntity"/>.</param>
	/// <returns>A disposable that can be used to unbind the state from the messenger.</returns>
	public static IDisposable Observe<TOther, TEntity, TKey>(
		this IMessenger messenger,
		IState<TEntity> state,
		IFeed<TOther> other,
		Func<TOther, TEntity, bool> predicate,
		Func<TEntity, TKey> keySelector)
	{
		return AttachedProperty.GetOrCreate(
			state,
			keySelector,
			(other, predicate, messenger),
			CreateRecipient);

		static Recipient<IState<TEntity>, TEntity, TKey> CreateRecipient(
			IState<TEntity> state,
			Func<TEntity, TKey> keySelector,
			(IFeed<TOther> other, Func<TOther, TEntity, bool> predicate, IMessenger messenger) config)
			=> new(state, config.messenger, keySelector, If<IState<TEntity>, TOther, TEntity, TKey>(config.other, config.predicate, StateExtensions.Update));
	}

	/// <summary>
	/// Listen for <see cref="EntityMessage{TEntity}"/> on the given <paramref name="messenger"/>, matches it with another value and updates the <paramref name="listState"/> accordingly.
	/// </summary>
	/// <typeparam name="TOther">Type of the other value to validate.</typeparam>
	/// <typeparam name="TEntity">Type of the value of the state.</typeparam>
	/// <typeparam name="TKey">Type of the identifier that uniquely identifies a <typeparamref name="TEntity"/>.</typeparam>
	/// <param name="messenger">The messenger to listen for <see cref="EntityMessage{TEntity}"/></param>
	/// <param name="listState">The list state to update.</param>
	/// <param name="other">The other value to validate with the updated entity using <paramref name="predicate"/>.</param>
	/// <param name="predicate">A predicted used with the updated entity to confirm that the change should by applied to the <paramref name="listState"/>.</param>
	/// <param name="keySelector">A selector to get a unique identifier of a <typeparamref name="TEntity"/>.</param>
	/// <returns>A disposable that can be used to unbind the state from the messenger.</returns>
	public static IDisposable Observe<TOther, TEntity, TKey>(
		this IMessenger messenger,
		IListState<TEntity> listState,
		IFeed<TOther> other,
		Func<TOther, TEntity, bool> predicate,
		Func<TEntity, TKey> keySelector)
	{
		return AttachedProperty.GetOrCreate(
			listState,
			keySelector,
			(other, predicate, messenger),
			CreateRecipient);

		static Recipient<IListState<TEntity>, TEntity, TKey> CreateRecipient(
			IListState<TEntity> state,
			Func<TEntity, TKey> keySelector,
			(IFeed<TOther> other, Func<TOther, TEntity, bool> predicate, IMessenger messenger) config)
			=> new(state, config.messenger, keySelector, If<IListState<TEntity>, TOther, TEntity, TKey>(config.other, config.predicate, StateExtensions.Update));
	}

	private static AsyncAction<TState, EntityMessage<TEntity>, Func<TEntity, TKey>> If<TState, TOther, TEntity, TKey>(
		IFeed<TOther> other,
		Func<TOther, TEntity, bool> predicate,
		AsyncAction<TState, EntityMessage<TEntity>, Func<TEntity, TKey>> update)
		=> async (state, message, keySelector, ct) =>
		{
			using var _ = SourceContext.GetOrCreate(other).AsCurrent();

			if ((await other.Data(ct)).IsSome(out var master) && predicate(master, message.Value))
			{
				await update(state, message, keySelector, ct);
			}
		};

	private sealed class Recipient<TState, TEntity, TKey> : IRecipient<EntityMessage<TEntity>>, IDisposable
		where TState : class
	{
		private readonly CancellationTokenSource _ct = new();
		private readonly TState _state;
		private readonly IMessenger _messenger;
		private readonly Func<TEntity, TKey> _keySelector;
		private readonly AsyncAction<TState, EntityMessage<TEntity>, Func<TEntity, TKey>> _update;

		public Recipient(
			TState state,
			IMessenger messenger,
			Func<TEntity, TKey> keySelector,
			AsyncAction<TState, EntityMessage<TEntity>, Func<TEntity, TKey>> update)
		{
			_state = state;
			_messenger = messenger;
			_keySelector = keySelector;
			_update = update;

			messenger.Register(this);
		}

		/// <inheritdoc />
		public async void Receive(EntityMessage<TEntity> msg)
		{
			try
			{
				await _update(_state, msg, _keySelector, _ct.Token);
			}
			catch (OperationCanceledException) when (_ct.IsCancellationRequested)
			{
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error(e, "Failed to apply update message.");
				}
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_messenger.Unregister<EntityMessage<TEntity>>(this);
			_ct.Cancel(throwOnFirstException: false);
			_ct.Dispose();
		}
	}
}
