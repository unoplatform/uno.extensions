using System;
using System.Linq;

namespace nVentive.Umbrella.Collections
{
	/// <summary>
	/// A group if items which notifies read and write operations.
	/// </summary>
	public interface IObservableGroup : IObservableCollection /*TODO, IKeyEquatable */
	{
		/// <summary>
		/// The key of the group
		/// </summary>
		object Key { get; }
	}
}
