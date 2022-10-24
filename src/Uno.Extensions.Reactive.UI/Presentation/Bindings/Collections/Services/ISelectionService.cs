using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings.Collections.Services;

internal interface ISelectionService
{
	/// <summary>
	/// Event raise when any properties of the service has changed
	/// </summary>
	event EventHandler StateChanged;

	/// <summary>
	/// Get the index of the primary selected item.
	/// </summary>
	uint? SelectedIndex { get; }

	/// <summary>
	/// Sets the **single** selected item.
	/// </summary>
	/// <param name="index">The index of the selected item or null to clear selection.</param>
	void SelectFromModel(uint? index);

	/// <summary>
	/// Sets the **single** selected item.
	/// </summary>
	/// <param name="index">The index of the selected item or -1 to clear selection.</param>
	void SelectFromView(int index);
}
