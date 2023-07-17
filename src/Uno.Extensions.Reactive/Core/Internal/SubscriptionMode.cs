using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// For a wrapper (e.g. a state) of a publisher (e.g. a feed), indicates how the underlying publisher should be subscribed.
/// </summary>
[Flags]
internal enum SubscriptionMode
{
	/// <summary>
	/// Underlying publisher is enumerated only once the wrapper is being enumerated.
	/// </summary>
	Lazy = 0,

	/// <summary>
	/// Underlying publisher is enumerated at the creation of the wrapper.
	/// </summary>
	Eager = 1,

	/// <summary>
	/// The enumeration of the underlying publisher is stopped as soon as the wrapper is no longer enumerated.
	/// </summary>
	RefCounted = 2,

	/// <summary>
	/// The default is Lazy, not ref counted.
	/// </summary>
	Default = Lazy,
}
