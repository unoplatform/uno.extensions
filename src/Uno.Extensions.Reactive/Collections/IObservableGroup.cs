using System;
using System.Linq;

namespace Uno.Extensions.Collections;

/// <summary>
/// A group if items which notifies read and write operations.
/// </summary>
internal interface IObservableGroup : IObservableCollection /*TODO, IKeyEquatable */
{
	/// <summary>
	/// The key of the group
	/// </summary>
	object Key { get; }
}
