using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Messaging;

/// <summary>
/// Defines possible types of updates for an entity
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
