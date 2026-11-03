using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings.Collections.Services;

internal interface ISelectionService : ISelectionInfo
{
	/// <summary>
	/// Event raise when any properties of the service has changed
	/// </summary>
	event EventHandler StateChanged;

	/// <summary>
	/// Gets a boolean which indicates if the source no longer holds the current selection (e.g. the volatile update which kept it has been dropped by a new message of the source).
	/// </summary>
	/// <remarks>
	/// The view keeps its selection as long as indexes are still valid (e.g. items added at the end of the list), but it must not be restored on a reset of the collection.
	/// </remarks>
	bool IsLostBySource { get; }

	/// <summary>
	/// Replace the current selection range with the specified item index range.
	/// </summary>
	void ReplaceRange(ItemIndexRange itemIndexRange);
}
