using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Umbrella.Presentation.Feeds.Collections;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.UI.WinUI.Presentation.Bindings;

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

		((State<IImmutableList<T>>)_state).GetSource(_ct.Token).ForEachAsync(
			msg => _items.Switch(new ImmutableObservableCollection<T>(msg.Current.Data.SomeOrDefault(ImmutableList<T>.Empty)!)),
			_ct.Token);

		// TODO: Quand le State est updated, on doit aussi updater les _items ... SYNC!
		// Il va aussi falloir qu'on se donne des ID de version pour pouvoir sync ces updates avec les message updates qu'on retourne dans le VRAI GetSource.

		// Mon immutable List<T> devrait être celle du BG thread ... enfin en fait celle du thread sur lequel tu fait le GetSource
		// Mon probléme c'est que je peux sur le UI thread je peux recevoir des collection aprés, donc mon immutable List va être décallée
		// Il faut que je publie une immutable List qui inclus tous les changements incluant ceux qui sont bufferred ....



		// En fait il faut que la collection publie elle même un version updated dés qu'elle à pu updater la collection sur le thread donné
		// Puis c'est celle que je pousse dans mon IFeed<IImmutableList<T>>

		// Dans le 
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
		return _state.GetSource(context, ct);

		//var collectionViewForCurrentThread = _items.GetForCurrentThread();
		//var localMsg = new MessageManager<IImmutableList<T>, ICollectionView>();

		//await foreach (var parentMsg in _state.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		//{
		//	// ICI on veut écouter le collection changed du collection view .... pour le thread voulu ... et faire un beau OnNext avec la collection updatée!


		//	if (localMsg.Update(current => current.With(parentMsg).Data(parentMsg.Current.Data.Map(t => collectionViewForCurrentThread))))
		//	{
		//		yield return localMsg.Current;
		//	}
		//}
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
