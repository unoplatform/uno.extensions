using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Operators;

internal sealed class ListFeedSelection<TSource, TOther> : IListState<TSource>, IStateImpl
{
	internal static MessageAxis<object?> SelectionUpdateSource { get; } = new(MessageAxes.SelectionSource, _ => null) { IsTransient = true };

	private readonly IListFeed<TSource> _source;
	private readonly IState<TOther> _selectionState;
	private readonly Func<IImmutableList<TSource>, SelectionInfo, Option<TOther>, Option<TOther>> _selectionToOther;
	private readonly Func<IImmutableList<TSource>, Option<TOther>, SelectionInfo> _otherToSelection;
	private readonly CancellationTokenSource _ct;
	private readonly string _name;
	private readonly SubscriptionMode _mode;

	private int _state = State.New;
	private IState<IImmutableList<TSource>>? _impl;

	private static class State
	{
		public const int New = 0;
		public const int Enabled = 1;
		public const int Disposed = int.MaxValue;
	}

	public ListFeedSelection(
		IListFeed<TSource> source,
		IState<TOther> selectionState,
		Func<IImmutableList<TSource>, SelectionInfo, Option<TOther>, Option<TOther>> selectionToOther,
		Func<IImmutableList<TSource>, Option<TOther>, SelectionInfo> otherToSelection,
		string logTag,
		SubscriptionMode mode = SubscriptionMode.Default)
	{
		_source = source;
		_selectionState = selectionState;
		_selectionToOther = selectionToOther;
		_otherToSelection = otherToSelection;
		_name = logTag;
		_mode = mode;

		// We must share the lifetime of the selectionState, so we share its Context.
		var ctx = ((IStateImpl)selectionState).Context;
		_ct = CancellationTokenSource.CreateLinkedTokenSource(ctx.Token); // We however allow early dispose of this state
		Context = ctx;

		if (mode is SubscriptionMode.Eager)
		{
			Enable();
		}
	}

	/// <inheritdoc />
	public SourceContext Context { get; }

	/// <inheritdoc />
	public IAsyncEnumerable<Message<IImmutableList<TSource>>> GetSource(SourceContext context, CancellationToken ct = default)
		=> Enable().GetSource(context, ct);

	/// <inheritdoc />
	public async ValueTask UpdateMessage(Action<MessageBuilder<IImmutableList<TSource>>> updater, CancellationToken ct)
		=> await Enable()
			.UpdateMessage(
				u =>
				{
					var currentData = u.CurrentData;
					var currentSelection = u.Get(MessageAxis.Selection);

					updater(u);

					var updatedData = u.CurrentData;
					var updatedSelection = u.Get(MessageAxis.Selection);

					var dataHasChanged = !OptionEqualityComparer<IImmutableList<TSource>>.RefEquals.Equals(updatedData, currentData);
					var selectionHasChanged = currentSelection != updatedSelection;

					if (dataHasChanged && !selectionHasChanged)
					{
						// The data has been updated, but not the selection.
						// While this could be valid, it might also cause some OutOfRange, so for now we just clear it.
						// TODO: This is only to ensure reliability, we should detect changes on the collection and update the SelectionInfo accordingly!
						u.Set(MessageAxis.Selection, MessageAxisValue.Unset, null);

						selectionHasChanged = true;
					}

					if (selectionHasChanged)
					{
						u.Set(SelectionUpdateSource, this);
					}
				},
				ct)
			.ConfigureAwait(false);

	private IState<IImmutableList<TSource>> Enable()
	{
		switch (Interlocked.CompareExchange(ref _state, State.Enabled, State.New))
		{
			case State.Disposed: throw new ObjectDisposedException(_name);
			case State.Enabled: return _impl!;
		}

		var impl = new StateImpl<IImmutableList<TSource>>(Context, _source.AsFeed(), SubscriptionMode.Eager);

		SelectionFeedUpdate? currentSelectionFromState = null;

		Context
			.GetOrCreateSource(_selectionState)
			.Where(msg => msg.Changes.Contains(MessageAxis.Data) && msg.Current.Get(SelectionUpdateSource) != this)
			.ForEachAwaitWithCancellationAsync(SyncFromStateToList, ConcurrencyMode.AbortPrevious, _ct.Token);

		Context
			.GetOrCreateSource(impl)
			.Where(msg => msg.Changes.Contains(MessageAxis.Selection) && msg.Current.Get(SelectionUpdateSource) != _selectionState)
			.ForEachAwaitWithCancellationAsync(SyncFromListToState, ConcurrencyMode.AbortPrevious, _ct.Token);

		return _impl = impl;

		async ValueTask SyncFromStateToList(Message<TOther> otherMsg, CancellationToken ct)
		{
			// Note: We sync only the SelectedItem. Error, Transient and any other axis are **not** expected to flow between the 2 sources.

			var updatedSelectionFromState = new SelectionFeedUpdate(this, otherMsg.Current.Data);

			if (currentSelectionFromState is not null)
			{
				impl.Inner.Replace(currentSelectionFromState, updatedSelectionFromState);
			}
			else
			{
				impl.Inner.Add(updatedSelectionFromState);
			}

			currentSelectionFromState = updatedSelectionFromState;
		}

		async ValueTask SyncFromListToState(Message<IImmutableList<TSource>> implMsg, CancellationToken ct)
		{
			var selectionInfo = implMsg.Current.GetSelectionInfo() ?? SelectionInfo.Empty;
			var items = implMsg.Current.Data.SomeOrDefault(ImmutableList<TSource>.Empty);

			await _selectionState
				.UpdateMessage(
					otherMsg =>
					{
						try
						{
							otherMsg
								.Set(SelectionUpdateSource, this)
								.Data(_selectionToOther(items, selectionInfo, otherMsg.CurrentData));
						}
						catch (Exception error)
						{
							this.Log().Error(error, $"Failed to push selection from the list to the state for {_name}.");
						}
					},
					ct)
				.ConfigureAwait(false);
		}
	}

	private class SelectionFeedUpdate : IFeedUpdate<IImmutableList<TSource>>
	{
		private readonly ListFeedSelection<TSource, TOther> _owner;
		private readonly Option<TOther> _other;

		public SelectionFeedUpdate(ListFeedSelection<TSource, TOther> owner, Option<TOther> other)
		{
			_owner = owner;
			_other = other;
		}

		public bool IsActive(Message<IImmutableList<TSource>>? parent, bool parentChanged, IMessageEntry<IImmutableList<TSource>> msg)
			=> !parentChanged || !(parent?.Changes.Contains(MessageAxis.Selection) ?? false);

		public void Apply(bool _, MessageBuilder<IImmutableList<TSource>, IImmutableList<TSource>> msg)
		{
			var items = msg.CurrentData.SomeOrDefault(ImmutableList<TSource>.Empty);
			var selection = _owner._otherToSelection(items, _other);

			msg
				.Set(MessageAxis.Selection, selection)
				.Set(SelectionUpdateSource, _owner._selectionState);
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		_state = State.Disposed;
		_ct.Cancel();
		if (_impl is not null)
		{
			await _impl.DisposeAsync();
		}
	}
}
