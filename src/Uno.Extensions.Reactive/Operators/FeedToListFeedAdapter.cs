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

internal class FeedToListFeedAdapter<T> : FeedToListFeedAdapter<IImmutableList<T>, T>
{
	public FeedToListFeedAdapter(IFeed<IImmutableList<T>> source, ItemComparer<T> itemComparer = default)
		: base(source, list => list, itemComparer)
	{
	}
}

// Note: This should **not** be a record as it causes some issues with mono runtime for Android and iOS
// which crashes when using instances of this record in dictionaries (caching) and break the AOT build.
// cf. https://github.com/unoplatform/Uno.Samples/issues/139

internal class FeedToListFeedAdapter<TCollection, TItem> : IListFeed<TItem>
{
	private readonly IFeed<TCollection> _source;
	private readonly Func<TCollection, IImmutableList<TItem>> _toImmutable;
	private readonly ItemComparer<TItem> _itemComparer;
	private readonly CollectionAnalyzer<TItem> _analyzer;

	public FeedToListFeedAdapter(
		IFeed<TCollection> source,
		Func<TCollection, IImmutableList<TItem>> toImmutable,
		ItemComparer<TItem> itemComparer = default)
	{
		_source = source;
		_toImmutable = toImmutable;
		_itemComparer = ListFeed<TItem>.GetComparer(itemComparer);
		_analyzer = ListFeed<TItem>.GetAnalyzer(itemComparer);
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<IImmutableList<TItem>>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		var localMsg = new MessageManager<TCollection, IImmutableList<TItem>>();
		await foreach (var parentMsg in context.GetOrCreateSource(_source).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(DoUpdate, parentMsg, ct))
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
			var updatedData = parentMsg.Current.Data.Map(_toImmutable);

			if (updatedData.IsSome(out var items) && items is null or { Count: 0 })
			{
				updatedData = Option<IImmutableList<TItem>>.None();
			}

			if (changeSet is not CollectionChangeSet)
			{
				try // As we might invoke the app Equality implementation, we make sure to try / catch it
				{
					changeSet = GetChangeSet(parentMsg.Previous.Data.Map(_toImmutable), updatedData);
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

	private protected CollectionChangeSet GetChangeSet(Option<IImmutableList<TItem>> previousData, Option<IImmutableList<TItem>> updatedData)
		=> GetChangeSet(_analyzer, previousData, updatedData);

	private protected static CollectionChangeSet GetChangeSet(CollectionAnalyzer<TItem> collectionAnalyzer, Option<IImmutableList<TItem>> previousData, Option<IImmutableList<TItem>> updatedData)
	{
		var hadItems = previousData.IsSome(out var previousItems);
		var hasItems = updatedData.IsSome(out var updatedItems);

		return (hadItems, hasItems) switch
		{
			(true, true) => collectionAnalyzer.GetChanges(previousItems, updatedItems),
			(true, false) => collectionAnalyzer.GetResetChange(previousItems, ImmutableList<TItem>.Empty),
			(false, true) => collectionAnalyzer.GetResetChange(ImmutableList<TItem>.Empty, updatedItems),
			(false, false) => CollectionChangeSet<TItem>.Empty,
		};
	}

	/// <inheritdoc />
	public override int GetHashCode()
		=> _source.GetHashCode()
			^ _toImmutable.GetHashCode()
			^ _itemComparer.GetHashCode();

	/// <inheritdoc />
	public override bool Equals(object? obj)
		=> obj is FeedToListFeedAdapter<TCollection, TItem> other && Equals(this, other);

	public static bool operator ==(FeedToListFeedAdapter<TCollection, TItem> left, FeedToListFeedAdapter<TCollection, TItem> right)
		=> Equals(left, right);

	public static bool operator !=(FeedToListFeedAdapter<TCollection, TItem> left, FeedToListFeedAdapter<TCollection, TItem> right)
		=> Equals(left, right);

	private protected static bool Equals(FeedToListFeedAdapter<TCollection, TItem> left, FeedToListFeedAdapter<TCollection, TItem> right)
		=> left._source.Equals(right._source)
			&& left._toImmutable.Equals(right._toImmutable)
			&& left._itemComparer.Equals(right._itemComparer);
}
