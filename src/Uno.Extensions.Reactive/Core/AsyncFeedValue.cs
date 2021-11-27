using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Enumerations to define the type of messages expected when awaiting a feed.
/// </summary>
[Flags]
public enum AsyncFeedValue
{
	/// <summary>
	/// Gets only message that are not transient and which does not encapsulates an error.
	/// </summary>
	Default = 0,

	/// <summary>
	/// Allows transient values.
	/// </summary>
	AllowTransient = 1,

	/// <summary>
	/// Allows messages that encapsulates an error.
	/// </summary>
	AllowError = 2,

	/// <summary>
	/// Gets all messages.
	/// </summary>
	All = AllowTransient | AllowError,
}
