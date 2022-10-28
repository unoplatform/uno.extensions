using System;
using System.Linq;

namespace Uno.Extensions.Equality;

/// <summary>
/// Defines an entity that has a constant logic identifier.
/// </summary>
/// <typeparam name="TKey">The type of the key</typeparam>
public interface IKeyed<out TKey>
	where TKey : notnull
{
	/// <summary>
	/// Gets the key identifier of this entity.
	/// </summary>
	TKey Key { get; }
}
