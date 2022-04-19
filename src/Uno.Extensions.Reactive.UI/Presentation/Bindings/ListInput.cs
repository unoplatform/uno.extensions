using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
internal record ListInput<T>(IInput<IImmutableList<T>> _implementation) : ListState<T>(_implementation), IListInput<T>
{
	private readonly IInput<IImmutableList<T>> _implementation = _implementation;

	/// <inheritdoc />
	public string PropertyName => _implementation.PropertyName;
}
