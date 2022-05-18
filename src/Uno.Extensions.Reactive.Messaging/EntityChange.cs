using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Messaging;

/// <summary>
/// Defines the possible of updates on an entity
/// </summary>
public enum EntityChange
{
	/// <summary>
	/// An entity has been created.
	/// </summary>
	Created,

	/// <summary>
	/// A new version of an entity is available.
	/// </summary>
	Updated,

	/// <summary>
	/// An entity has been removed.
	/// </summary>
	Deleted,
}
