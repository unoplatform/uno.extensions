using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Bindings.Collections;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// An helper class use to data-bind a <see cref="IListFeed{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public sealed partial class BindableListFeed<T> : ISignal<IMessage>, IListState<T>, IInput<IImmutableList<T>>
{
	private readonly CancellationTokenSource _ct = new();
	private readonly BindableCollection _items;
	private readonly IListState<T> _state;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="propertyName">The name of the property backed by the object.</param>
	/// <param name="source">The source data stream.</param>
	/// <param name="ctx">The context of the owner.</param>
	public BindableListFeed(string propertyName, IListFeed<T> source, SourceContext ctx)
	{
		PropertyName = propertyName;
		_items = BindableCollection.Create<T>();
		_state = ctx.GetOrCreateListState(source);

		_state.GetSource(ctx, _ct.Token).ForEachAsync(
			msg => _items.Switch(new ImmutableObservableCollection<T>(msg.Current.Data.SomeOrDefault(ImmutableList<T>.Empty))),
			_ct.Token);

		// Note: we have to listen for collection changes on bindable _items to update the state.
		// https://github.com/unoplatform/uno.extensions/issues/370
	}

	/// <inheritdoc />
	public string PropertyName { get; }

	/// <inheritdoc />
	async IAsyncEnumerable<IMessage> ISignal<IMessage>.GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct)
	{
		// This is the GetSource implementation dedicated to data binding!
		// Instead of being an IFeed<IIMutableList<T>> we are instead exposing an IFeed<ICollectionView>.
		// WARNING: **The ICollectionView is mutable**

		var collectionViewForCurrentThread = _items.GetForCurrentThread();
		var localMsg = new MessageManager<IImmutableList<T>, ICollectionView>();

		await foreach (var parentMsg in _state.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(
					(current, @params) => current
						.With(@params.parentMsg)
						.Data(@params.parentMsg.Current.Data.Map(_ => @params.collectionViewForCurrentThread)),
					(parentMsg, collectionViewForCurrentThread)))
			{
				yield return localMsg.Current;
			}
		}
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		// TODO Uno: Should the source be per thread? This is actually not used for bindings.

		return _state.GetSource(context, ct);
	}

	/// <inheritdoc />
	ValueTask IListState<T>.UpdateMessage(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _state.UpdateMessage(updater, ct);

	/// <inheritdoc />
	ValueTask IState<IImmutableList<T>>.UpdateMessage(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _state.UpdateMessage(updater, ct);

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		_ct.Cancel(false);
		await _state.DisposeAsync();
	}
}
