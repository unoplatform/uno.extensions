using System;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Differential;

/// <summary>
/// A collection backed by <see cref="IDifferentialCollectionNode"/>
/// </summary>
internal interface IDifferentialCollection
{
	/// <summary>
	/// Gets the head node of the collection.
	/// </summary>
	IDifferentialCollectionNode Head { get; }
}
