using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Tests._Utils;

internal static class FuncEqualityComparer<T>
{
	public static IEqualityComparer<T> Create<TProperty>(Func<T, TProperty> selector)
		where TProperty : notnull
		=> new FuncEqualityComparer<T, TProperty>(selector);
}

internal class FuncEqualityComparer<T, TProperty> : IEqualityComparer<T>
	where TProperty : notnull
{
	private readonly Func<T, TProperty> _selector;

	public FuncEqualityComparer(Func<T, TProperty> selector)
	{
		_selector = selector;
	}

	/// <inheritdoc />
	public bool Equals(T? x, T? y)
		=> (x, y) switch
		{
			(null, null) => true,
			(null, _) => false,
			(_, null) => false,
			(_, _) => _selector(x).Equals(_selector(y)),
		};

	/// <inheritdoc />
	public int GetHashCode(T obj)
		=> _selector(obj).GetHashCode();
}
