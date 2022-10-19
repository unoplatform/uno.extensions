using System;
using System.Linq;

namespace Uno.Extensions.Edition;

/// <summary>
/// An implementation of <see cref="IValueAccessor{TOwner, TValue}"/> built using delegates for getter and setter.
/// </summary>
/// <typeparam name="TOwner"></typeparam>
/// <typeparam name="TValue"></typeparam>
public sealed class ValueAccessor<TOwner, TValue> : IValueAccessor<TOwner, TValue>
{
	private readonly Func<TOwner, TValue> _getter;
	private readonly Func<TOwner, TValue, TOwner> _setter;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="path">The path of the value edited through this accessor.</param>
	/// <param name="getter">The get method.</param>
	/// <param name="setter">The set method.</param>
	public ValueAccessor(string path, Func<TOwner, TValue> getter, Func<TOwner, TValue, TOwner> setter)
	{
		Path = path;
		_getter = getter;
		_setter = setter;
	}

	/// <inheritdoc />
	public string Path { get; }

	/// <inheritdoc />
	public TValue Get(TOwner entity)
		=> _getter(entity);

	/// <inheritdoc />
	public TOwner Set(TOwner entity, TValue value)
		=> _setter(entity, value);
}
