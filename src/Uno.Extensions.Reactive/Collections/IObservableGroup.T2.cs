using System;
using System.Linq;

namespace nVentive.Umbrella.Collections
{
	/// <summary>
	/// A group of <typeparamref name="TItem"/> which notifies read and write oprations.
	/// </summary>
	/// <typeparam name="TKey">Type of the key</typeparam>
	/// <typeparam name="TItem">Type of the items</typeparam>
	public interface IObservableGroup<TKey, TItem> : IObservableGroup<TItem>, IGrouping<TKey, TItem>/* TODO, IKeyEquatable<IObservableGroup<TKey, TItem>>
		where TKey : IKeyEquatable<TKey>*/
	{
		// TKey Key { get; } => Added by the IGrouping<TKet, TIem>
	}
}
