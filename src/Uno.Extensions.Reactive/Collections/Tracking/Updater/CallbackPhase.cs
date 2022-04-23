using System;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

[Flags]
internal enum CallbackPhase
{
	/// <summary>
	/// Before raising the change event
	/// </summary>
	Before = 1,

	/// <summary>
	/// At (almost) the same time as the event (a.k.a. 'instead of')
	/// </summary>
	Main = 2,

	/// <summary>
	/// After raising the change event
	/// </summary>
	After = 4,

	/// <summary>
	/// All phases at once
	/// </summary>
	All = Before | Main | After,
}
