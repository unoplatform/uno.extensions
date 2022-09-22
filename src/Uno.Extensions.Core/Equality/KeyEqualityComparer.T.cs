using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Equality;

/// <summary>
/// An <see cref="IEqualityComparer{T}"/> which will consider as equals to instance of T as soon has their key identifier are equals.
/// </summary>
/// <typeparam name="T">Type of compared objects.</typeparam>
public sealed class KeyEqualityComparer<T> : IEqualityComparer<T>, IEqualityComparer
	where T : IKeyEquatable<T>
{
	/// <summary>
	/// Gets an <see cref="IEqualityComparer{T}"/> which compares only the key,
	/// if <typeparamref name="T"/> implements <see cref="IKeyEquatable{T}"/>.
	/// </summary>
	/// <returns>
	/// An <see cref="IEqualityComparer{T}"/> which compares only the key,
	/// if <typeparamref name="T"/> implements <see cref="IKeyEquatable{T}"/>,
	/// `null` otherwise.
	/// </returns>
	public static IEqualityComparer<T>? Find()
		=> KeyEqualityComparer.Find<T>();

	/// <inheritdoc />
	bool IEqualityComparer.Equals(object? x, object? y)
		=> x is T xT ? y is T yT && xT.KeyEquals(yT) : y is null;

	/// <inheritdoc />
	public bool Equals(T? x, T? y)
		=> x is not null ? y is not null && x.KeyEquals(y) : y is null;

	/// <inheritdoc />
	int IEqualityComparer.GetHashCode(object? obj)
		=> obj is T t ? t.GetKeyHashCode() : -1;

	/// <inheritdoc />
	public int GetHashCode(T? obj)
		=> obj is not null ? obj.GetKeyHashCode() : -1;
}
