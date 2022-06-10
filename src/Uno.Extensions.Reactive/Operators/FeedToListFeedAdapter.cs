using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Operators;

internal record FeedToListFeedAdapter<T>(
	IFeed<IImmutableList<T>> Source,
	ItemComparer<T> ItemComparer = default)
	: FeedToListFeedAdapter<IImmutableList<T>, T>(Source, list => list, ItemComparer);

internal record FeedToListFeedAdapter<TCollection, TItem>(
	IFeed<TCollection> Source,
	Func<TCollection, IImmutableList<TItem>> ToImmutable,
	ItemComparer<TItem> ItemComparer = default)
	: IListFeed<TItem>
{
	private readonly CollectionAnalyzer<TItem> _analyzer = new(ItemComparer);

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<IImmutableList<TItem>>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		var localMsg = new MessageManager<TCollection, IImmutableList<TItem>>();
		await foreach (var parentMsg in Source.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(DoUpdate, parentMsg))
			{
				yield return localMsg.Current;
			}
		}
	}

	private MessageBuilder<TCollection, IImmutableList<TItem>> DoUpdate(MessageManager<TCollection, IImmutableList<TItem>>.CurrentMessage current, Message<TCollection> parentMsg)
	{
		var updated = current.With(parentMsg!);

		if (parentMsg.Changes.Contains(MessageAxis.Data, out var changeSet))
		{
			var error = default(Exception);
			var updatedData = parentMsg.Current.Data.Map(ToImmutable);

			if (updatedData.IsSome(out var items) && items is null or { Count: 0 })
			{
				updatedData = Option<IImmutableList<TItem>>.None();
			}

			if (changeSet is not CollectionChangeSet)
			{
				try // As we might invoke the app Equality implementation, we make sure to try / catch it
				{
					changeSet = GetChangeSet(_analyzer, parentMsg.Previous.Data.Map(ToImmutable), updatedData);
				}
				catch (Exception e)
				{
					error = e;
					updatedData = Option<IImmutableList<TItem>>.Undefined();
					changeSet = null;
				}
			}

			updated
				.Data(updatedData, changeSet)
				.Error(error);
		}

		return updated;
	}

	internal static CollectionChangeSet GetChangeSet(Option<IImmutableList<TItem>> previousData, Option<IImmutableList<TItem>> updatedData)
		=> GetChangeSet(CollectionAnalyzer<TItem>.Default, previousData, updatedData);

	internal static CollectionChangeSet GetChangeSet(CollectionAnalyzer<TItem> collectionAnalyzer, Option<IImmutableList<TItem>> previousData, Option<IImmutableList<TItem>> updatedData)
	{
		var hadItems = previousData.IsSome(out var previousItems);
		var hasItems = updatedData.IsSome(out var updatedItems);

		return ((hadItems, hasItems)) switch
		{
			(true, true) => collectionAnalyzer.GetChanges(previousItems, updatedItems),
			(true, false) => collectionAnalyzer.GetReset(previousItems, ImmutableList<TItem>.Empty),
			(false, true) => collectionAnalyzer.GetReset(ImmutableList<TItem>.Empty, updatedItems),
			(false, false) => CollectionChangeSet.Empty,
		};
	}
}
