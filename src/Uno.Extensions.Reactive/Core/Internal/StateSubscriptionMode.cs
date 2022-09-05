using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

[Flags]
internal enum StateSubscriptionMode
{
	/// <summary>
	/// Underlying feed is enumerated only once the state is being enumerated.
	/// </summary>
	Lazy = 0,

	/// <summary>
	/// Underlying feed is enumerated at the creation of the state.
	/// </summary>
	Eager = 1,

	/// <summary>
	/// The enumeration of the underlying feed is stopped as soon as this state is no longer enumerated.
	/// </summary>
	RefCounted = 2,

	/// <summary>
	/// The default is Lazy, not ref counted.
	/// </summary>
	Default = Lazy,
}
