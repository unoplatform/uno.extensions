using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

internal record ListFeedImpl<T>(IFeed<IImmutableList<T>> implementation) : IListFeed<T>, IListFeedWrapper<T>
{
	private readonly IFeed<IImmutableList<T>> implementation = implementation;
	private readonly CollectionAnalyzer<T> _analyzer = new();

	IFeed<IImmutableList<T>> IListFeedWrapper<T>.Source => implementation;

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		var localMsg = new MessageManager<IImmutableList<T>, IImmutableList<T>>();
		await foreach (var parentMsg in implementation.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(DoUpdate))
			{
				yield return localMsg.Current;
			}

			MessageBuilder<IImmutableList<T>, IImmutableList<T>> DoUpdate(MessageManager<IImmutableList<T>, IImmutableList<T>>.CurrentMessage current)
			{
				var updated = current.With(parentMsg!);

				if (parentMsg!.Changes.Contains(MessageAxis.Data, out var changeSet))
				{
					var error = default(Exception);
					var updatedData = parentMsg.Current.Data;
					if (changeSet is not CollectionChangeSet)
					{
						try // As we might invoke the app Equality implementation, we make sure to try / catch it
						{
							changeSet = GetChangeSet(parentMsg.Previous.Data, updatedData);
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
		}
	}

	private CollectionChangeSet GetChangeSet(Option<IImmutableList<T>> previousData, Option<IImmutableList<T>> updatedData)
	{
		var hadItems = previousData.IsSome(out var previousItems);
		var hasItems = updatedData.IsSome(out var updatedItems);

		return ((hadItems, hasItems)) switch
		{
			(true, true) => _analyzer.GetChanges(previousItems.ToList(), updatedItems.ToList()),
			(true, false) => _analyzer.GetReset(previousItems.ToList(), Array.Empty<T>()),
			(false, true) => _analyzer.GetReset(Array.Empty<T>(), updatedItems.ToList()),
			(false, false) => CollectionChangeSet.Empty,
		};
	}
}
