using System;
using System.Linq;

namespace Uno.Extensions.Collections;

/// <summary>
/// A group of <typeparamref name="TItem"/> which notifies read and write operations.
/// </summary>
/// <typeparam name="TKey">Type of the key</typeparam>
/// <typeparam name="TItem">Type of the items</typeparam>
internal interface IObservableGroup<TKey, TItem> : IObservableGroup<TItem>, IGrouping<TKey, TItem>/* TODO uno, IKeyEquatable<IObservableGroup<TKey, TItem>>
		where TKey : IKeyEquatable<TKey>*/
{
	// TKey Key { get; } => Added by the IGrouping<TKet, TIem>
}
