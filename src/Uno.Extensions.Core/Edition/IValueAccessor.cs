using System;
using System.Linq;

namespace Uno.Extensions.Edition;

/// <summary>
/// An abstracted way to edit a edit a value on a given owner.
/// </summary>
/// <typeparam name="TOwner">Type of the entity which owns the value.</typeparam>
/// <typeparam name="TValue">Type of the value.</typeparam>
public interface IValueAccessor<TOwner, TValue>
{
	/// <summary>
	/// Gets the path of the 'value' relative to the 'owner'.
	/// This contains the C# path to get the value of an owner, e.g. "PropertyName" when this accessor describes a `new PropertySelector(t => t.PropertyName)`.
	/// This is should be used only for debug purposes.
	/// </summary>
	public string Path { get; }

	/// <summary>
	/// Gets the current value for the given entity.
	/// </summary>
	/// <param name="entity">The entity for which to get value for.</param>
	/// <returns>The current value.</returns>
	public TValue Get(TOwner entity);

	/// <summary>
	/// Creates an updated instance of the entity with the given value.
	/// </summary>
	/// <param name="entity">The current entity.</param>
	/// <param name="value">The new value to set.</param>
	/// <returns>An updated entity with the updated value.</returns>
	public TOwner Set(TOwner entity, TValue value);
}
