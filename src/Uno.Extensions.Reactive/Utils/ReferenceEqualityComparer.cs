using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Utils;

internal class ReferenceEqualityComparer<T> : IEqualityComparer<T>
{
	public static ReferenceEqualityComparer<T> Default { get; } = new();

	private ReferenceEqualityComparer()
	{
	}

	/// <inheritdoc />
	public bool Equals(T? x, T? y)
		=> ReferenceEquals(x, y);

	/// <inheritdoc />
	public int GetHashCode(T obj)
		=> obj?.GetHashCode() ?? 0;
}
