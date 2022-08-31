using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

internal enum StateUpdateKind
{
	/// <summary>
	/// The updates made on State are flushed as soon as the source feed produces a new value
	/// </summary>
	Volatile,

	/// <summary>
	/// The updates made on State are re-applied on each message produced by the source feed,
	/// until update is being explicitly removed or reports as inactive.
	/// </summary>
	Persistent
}
