using System;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

sealed partial class CollectionUpdater
{
	/// <summary>
	/// An handler of changes detected by a <see cref="CollectionAnalyzer"/>.
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
