using System;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

public sealed partial class CollectionChangesQueue
{
	/// <summary>
	/// An handler of changes detected by a <see cref="CollectionTracker"/>.
	/// </summary>
	public interface IHandler
	{
		/// <summary>
		/// Notify the target collection to raise a collection changed event args
		/// </summary>
		void Raise(RichNotifyCollectionChangedEventArgs args);

		/// <summary>
		/// Notify the target collection to apply a change, but as it was already handled by callbacks, the provided event args must not be raised.
		/// </summary>
		void ApplySilently(RichNotifyCollectionChangedEventArgs args);
	}
}
