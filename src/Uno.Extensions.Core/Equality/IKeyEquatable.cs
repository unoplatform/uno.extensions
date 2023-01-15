using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Equality;

/// <summary>
/// Defines a generalized method that a value type or class implements to create a type-specific method for determining "key" equality of instances.
/// Unlike <see cref="IEquatable{T}"/>, this is expected to compare only the "key" of the instance.
/// The "key" is a logic identifier of an entity which remains the same for multiple versions of the same entity.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IKeyEquatable<in T>
{
	/// <summary>
	/// Gets the hash code of the "key" of the entity.
	/// </summary>
	/// <returns></returns>
	int GetKeyHashCode();

	/// <summary>
	/// Indicates whether the current object has the same key to another object of the same type.
	/// </summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object has the same key with the other parameter; otherwise, false.</returns>
	bool KeyEquals(T other);
}
