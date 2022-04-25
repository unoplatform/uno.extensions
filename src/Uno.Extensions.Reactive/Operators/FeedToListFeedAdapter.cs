using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Operators;

internal record FeedToListFeedAdapter<T>(IFeed<IImmutableList<T>> Source) : IListFeed<T>
{
	private readonly CollectionAnalyzer<T> _analyzer = new();

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		var localMsg = new MessageManager<IImmutableList<T>, IImmutableList<T>>();
		await foreach (var parentMsg in Source.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(DoUpdate, parentMsg))
			{
				yield return localMsg.Current;
			}
		}
	}

	private MessageBuilder<IImmutableList<T>, IImmutableList<T>> DoUpdate(MessageManager<IImmutableList<T>, IImmutableList<T>>.CurrentMessage current, Message<IImmutableList<T>> parentMsg)
	{
		var updated = current.With(parentMsg!);

		if (parentMsg.Changes.Contains(MessageAxis.Data, out var changeSet))
		{
			var error = default(Exception);
			var updatedData = parentMsg.Current.Data;

			if (updatedData.IsSome(out var items) && items is null or { Count: 0 })
			{
				updatedData = Option<IImmutableList<T>>.None();
			}

			if (changeSet is not CollectionChangeSet)
			{
				try // As we might invoke the app Equality implementation, we make sure to try / catch it
				{
					changeSet = GetChangeSet(_analyzer, parentMsg.Previous.Data, updatedData);
				}
				catch (Exception e)
				{
					error = e;
					updatedData = Option<IImmutableList<T>>.Undefined();
					changeSet = null;
				}
			}

			updated
				.Data(updatedData, changeSet)
				.Error(error);
		}

		return updated;
	}

	internal static CollectionChangeSet GetChangeSet(Option<IImmutableList<T>> previousData, Option<IImmutableList<T>> updatedData)
		=> GetChangeSet(CollectionAnalyzer<T>.Default, previousData, updatedData);

	internal static CollectionChangeSet GetChangeSet(CollectionAnalyzer<T> collectionAnalyzer, Option<IImmutableList<T>> previousData, Option<IImmutableList<T>> updatedData)
	{
		var hadItems = previousData.IsSome(out var previousItems);
		var hasItems = updatedData.IsSome(out var updatedItems);

		return ((hadItems, hasItems)) switch
		{
			(true, true) => collectionAnalyzer.GetChanges(previousItems, updatedItems),
			(true, false) => collectionAnalyzer.GetReset(previousItems, ImmutableList<T>.Empty),
			(false, true) => collectionAnalyzer.GetReset(ImmutableList<T>.Empty, updatedItems),
			(false, false) => CollectionChangeSet.Empty,
		};
	}
}
