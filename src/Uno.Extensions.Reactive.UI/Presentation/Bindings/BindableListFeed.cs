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

public sealed partial class BindableListFeed<T> : ISignal<IMessage>, IListInput<T>, IInput<IImmutableList<T>>
{
	private readonly CancellationTokenSource _ct = new();
	private readonly BindableCollection _items;
	private readonly IState<IImmutableList<T>> _state;

	public BindableListFeed(string propertyName, IListFeed<T> source, SourceContext ctx)
	{
		PropertyName = propertyName;
		_items = BindableCollection.Create<T>();
		_state = ctx.GetOrCreateState(source.AsFeed());

		((StateImpl<IImmutableList<T>>)_state).GetSource(_ct.Token).ForEachAsync(
			msg => _items.Switch(new ImmutableObservableCollection<T>(msg.Current.Data.SomeOrDefault(ImmutableList<T>.Empty)!)),
			_ct.Token);

		// TODO Uno: we have to listen for collection changes on bindable _items to update the state.
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
			if (localMsg.Update(current => current.With(parentMsg).Data(parentMsg.Current.Data.Map(_ => collectionViewForCurrentThread))))
			{
				yield return localMsg.Current;
			}
		}
	}

	public IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		// TODO Uno: Should the source be per thread? This is actually not used for bindings.

		return _state.GetSource(context, ct);
	}

	/// <inheritdoc />
	ValueTask IListState<T>.Update(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _state.Update(updater, ct);

	/// <inheritdoc />
	ValueTask IState<IImmutableList<T>>.Update(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _state.Update(updater, ct);

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		_ct.Cancel(false);
		await _state.DisposeAsync();
	}
}
