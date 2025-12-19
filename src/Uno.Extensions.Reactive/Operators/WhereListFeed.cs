using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Operators;

internal sealed class WhereListFeed<
	[DynamicallyAccessedMembers(ListFeed.TRequirements)]
	T
> : IListFeed<T>
{
	private static readonly CollectionAnalyzer<T> _analyzer = ListFeed<T>.DefaultAnalyzer;

	private readonly IListFeed<T> _parent;
	private readonly Predicate<T> _predicate;

	public WhereListFeed(IListFeed<T> parent, Predicate<T> predicate)
	{
		_parent = parent;
		_predicate = predicate;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<IImmutableList<T>>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		var localMsg = new MessageManager<IImmutableList<T>, IImmutableList<T>>();
		await foreach (var parentMsg in context.GetOrCreateSource(_parent).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(DoUpdate, parentMsg, ct))
			{
				yield return localMsg.Current;
			}
		}
	}

	private MessageBuilder<IImmutableList<T>, IImmutableList<T>> DoUpdate(MessageManager<IImmutableList<T>, IImmutableList<T>>.CurrentMessage current, Message<IImmutableList<T>> parentMsg)
	{
		var updated = current.With(parentMsg!);

		if (parentMsg!.Changes.Contains(MessageAxis.Data))
		{
			var previousFilteredItems = updated.CurrentData.SomeOrDefault(ImmutableList<T>.Empty);
			var data = parentMsg.Current.Data;
			var updatedItems = data.SomeOrDefault(ImmutableList<T>.Empty);

			switch (data.Type)
			{
				case OptionType.Undefined:
					updated
						.Data(Option.Undefined<IImmutableList<T>>(), _analyzer.GetResetChange(previousFilteredItems, ImmutableList<T>.Empty))
						.Error(null);
					break;

				case OptionType.None:
					updated
						.Data(Option.None<IImmutableList<T>>(), _analyzer.GetResetChange(previousFilteredItems, ImmutableList<T>.Empty))
						.Error(null);
					break;

				case OptionType.Some:
					try
					{
						// TODO uno: use CollectionChangeSet to not re-enumerate all items
						//var changes = changeSet as CollectionChangeSet;
						//if (changes is null)
						//{
						//	this.Log().Warn(
						//		$"[PERFORMANCE] The list feed {_parent} produced a message which does not contains collection change-set. "
						//		+ "It means that all sub-operators needs to compute it.");

						//	changes = FeedToListFeedAdapter<T>.GetChangeSet(parentMsg.Previous.Data, data);
						//}

						var updatedFilteredItems = updatedItems.Where(item => _predicate(item)).ToImmutableList() as IImmutableList<T>;
						if (updatedFilteredItems is { Count: 0 })
						{
							updated
								.Data(Option.None<IImmutableList<T>>(), _analyzer.GetResetChange(previousFilteredItems, ImmutableList<T>.Empty))
								.Error(null);
						}
						else
						{
							var changes = _analyzer.GetChanges(previousFilteredItems, updatedFilteredItems);
							updated
								.Data(Option.Some(updatedFilteredItems), changes)
								.Error(null);
						}
					}
					catch (Exception error)
					{
						updated
							.Data(Option.Undefined<IImmutableList<T>>())
							.Error(error);
					}
					break;
			}
		}

		return updated;
	}
}
