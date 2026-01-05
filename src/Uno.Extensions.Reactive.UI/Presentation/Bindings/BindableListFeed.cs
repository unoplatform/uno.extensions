using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Bindings.Collections;
using Uno.Extensions.Reactive.Bindings.Collections.Services;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.UI.Utils;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// An helper class use to data-bind a <see cref="IListFeed{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the items.</typeparam>
public sealed partial class BindableListFeed<T> : ISignal<IMessage>, IListState<T>, IInput<IImmutableList<T>>
{
	private readonly BindableCollection _items;
	private readonly IListState<T> _state;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="propertyName">The name of the property backed by the object.</param>
	/// <param name="source">The source data stream.</param>
	/// <param name="ctx">The context of the owner.</param>
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(BindableListFeed<>))]
	public BindableListFeed(string propertyName, IListFeed<T> source, SourceContext ctx)
	{
		PropertyName = propertyName;

		_state = ctx.GetOrCreateListState(source);
		_items = CreateBindableCollection(_state);
	}

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="propertyName">The name of the property backed by the object.</param>
	/// <param name="source">The source data stream.</param>
	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(BindableListFeed<>))]
	public BindableListFeed(string propertyName, IListState<T> source)
	{
		PropertyName = propertyName;

		_state = source;
		_items = CreateBindableCollection(source);
	}

	/// <inheritdoc />
	SourceContext IState.Context => _state.Context;

	/// <inheritdoc />
	IRequestSource IState.Requests => _state.Requests;

	/// <inheritdoc />
	public string PropertyName { get; }

	/// <inheritdoc />
	async IAsyncEnumerable<IMessage> ISignal<IMessage>.GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct)
	{
		// This is the GetSource implementation dedicated to data binding!
		// Instead of being an IFeed<IIMutableList<T>> we are instead exposing an IFeed<ICollectionView>.
		// WARNING: **The ICollectionView is mutable**

		var collectionView = context.FindDispatcher() is {} dispatcher
			? _items.GetFor(dispatcher)
			: _items.GetForCurrentThread();
		var localMsg = new MessageManager<IImmutableList<T>, ICollectionView>();

		await foreach (var parentMsg in _state.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(
					(current, @params) => current
						.With(@params.parentMsg)
						.Data(@params.parentMsg.Current.Data.Map(_ => @params.collectionView)),
					(parentMsg, collectionView),
					ct))
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
	ValueTask IListState<T>.UpdateMessageAsync(Action<MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _state.UpdateMessageAsync(updater, ct);

	/// <inheritdoc />
	ValueTask IState<IImmutableList<T>>.UpdateMessageAsync(Action<MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct)
		=> _state.UpdateMessageAsync(updater, ct);


	private static BindableCollection CreateBindableCollection(IListState<T> state)
	{
		var ctx = state.Context;
		var currentCount = 0;
		var pageTokens = new TokenSetAwaiter<PageToken>();

		var collection = default(BindableCollection);
		var requests = new RequestSource();
		var pagination = new PaginationService(LoadMore);
		var selection = new SelectionService(SetSelected);
		var edition = new EditionService(Edit);
		var services = new SingletonServiceProvider(pagination, selection, edition);

		collection = BindableCollection.Create(
			services: services,
			itemComparer: ListFeed<T>.DefaultComparer);

		if (ctx.Token.CanBeCanceled)
		{
			ctx.Token.Register(pagination.Dispose);
			ctx.Token.Register(requests.Dispose);
			ctx.Token.Register(() => _ = services.DisposeAsync());
		}

		// Note: we have to listen for collection changes on bindable _items to update the state.
		// https://github.com/unoplatform/uno.extensions/issues/370

		state.GetSource(SourceContextHelper.CreateChildContext(ctx, collection, state, requests), ctx.Token).ForEachAsync(
			msg =>
			{
				if (ctx.Token.IsCancellationRequested)
				{
					return;
				}

				var items = default(IImmutableList<T>);
				if (msg.Changes.Contains(MessageAxis.Data, out var changes))
				{
					items = msg.Current.Data.SomeOrDefault(ImmutableList<T>.Empty);
					currentCount = items.Count;

					collection.Switch(new ImmutableObservableCollection<T>(items), changes as CollectionChangeSet);
				}

				var pageInfo = msg.Current.GetPaginationInfo();

				// Clear the loading state
				if (pageInfo is not null)
				{
					pagination.HasMoreItems = pageInfo.HasMoreItems;
					pagination.IsLoadingMoreItems = pageInfo.IsLoadingMoreItems;
				}

				// Then complete the loading request
				if (!msg.Current.IsTransient && pageInfo is { IsLoadingMoreItems: false } page)
				{
					pageTokens.Received(page.Tokens);
				}

				if (msg.Changes.Contains(MessageAxis.Selection)
					&& msg.Current.GetSelectionInfo() is { } selectionInfo
					&& msg.Current.Get(BindableViewModelBase.BindingSource) != collection)
				{
					selection.SetFromSource(selectionInfo);
				}
			},
			ctx.Token);

		async ValueTask<uint> LoadMore(uint desiredCount, CancellationToken ct)
		{
			var originalCount = currentCount;

			await pageTokens.WaitFor(requests.RequestMoreItems(desiredCount), ct).ConfigureAwait(false);

			var resultCount = currentCount;

			return (uint)Math.Max(0, resultCount - originalCount);
		}

		ValueTask SetSelected(SelectionInfo info, CancellationToken ct)
			=> state.UpdateMessageAsync(msg => msg.Selected(info).Set(BindableViewModelBase.BindingSource, collection), ct);

		ValueTask Edit(Func<IDifferentialCollectionNode, IDifferentialCollectionNode> change, CancellationToken ct)
			=> state.UpdateMessageAsync(
				msg =>
				{
					// Note: The change might have been computed on an older version of the collection
					// We are NOT validating this. It means that we cou;d get an out-of-range for remove for instance.

					var currentItems = msg.CurrentData.SomeOrDefault();
					var currentHead = currentItems switch
					{
						IDifferentialCollection diffCollection => diffCollection.Head,
						null => new EmptyNode(),
						_ => new ResetNode<T>(currentItems)
					};
					var newHead = change(currentHead);
					var newItems = new DifferentialImmutableList<T>(newHead) as IImmutableList<T>;

					msg.Data(newItems).Set(BindableViewModelBase.BindingSource, collection);
				},
				ct);

		return collection;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _state.DisposeAsync().ConfigureAwait(false);
	}
}
