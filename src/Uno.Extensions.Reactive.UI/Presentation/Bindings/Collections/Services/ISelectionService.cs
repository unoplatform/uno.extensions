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
	/// Replace the selected range.
	/// </summary>
	void ReplaceRange(ItemIndexRange itemIndexRange);
}
