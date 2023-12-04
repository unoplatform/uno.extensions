using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Equality;

/// <summary>
/// Extensions over <see cref="IEqualityComparer{T}"/>.
/// </summary>
public static class EqualityComparerExtensions
{
	/// <summary>
	/// Adapts a generic <see cref="IEqualityComparer{T}"/> to an untyped <see cref="IEqualityComparer"/>.
	/// </summary>
	/// <typeparam name="T">Type of the compared objects.</typeparam>
	/// <param name="comparer">The comparer to adapt.</param>
	/// <returns>
	/// The given comparer itself if it also implement <see cref="IEqualityComparer"/>
	/// or a wrapper that encapsulate it hiding the generic parameter.
	/// </returns>
	public static IEqualityComparer ToEqualityComparer<T>(this IEqualityComparer<T> comparer)
		=> comparer as IEqualityComparer ?? new TypedToUntypedEqualityComparerAdapter<T>(comparer);

	/// <summary>
	/// Adapts an untyped <see cref="IEqualityComparer"/> to an generic <see cref="IEqualityComparer{T}"/>.
	/// </summary>
	/// <typeparam name="T">Type of the compared objects.</typeparam>
	/// <param name="comparer">The comparer to adapt.</param>
	/// <returns>
	/// The given comparer itself if it also implement <see cref="IEqualityComparer{T}"/>
	/// or a wrapper that encapsulate it hiding the generic parameter.
	/// </returns>
	public static IEqualityComparer<T> ToEqualityComparer<T>(this IEqualityComparer comparer)
		=> comparer as IEqualityComparer<T> ?? new UntypedToTypedEqualityComparerAdapter<T>(comparer);

	private class TypedToUntypedEqualityComparerAdapter<T> : IEqualityComparer<T>, IEqualityComparer
	{
		private readonly IEqualityComparer<T> _comparer;

		public TypedToUntypedEqualityComparerAdapter(IEqualityComparer<T> comparer)
		{
			_comparer = comparer;
		}

		/// <inheritdoc />
		public bool Equals(T? x, T? y)
			=> x is { } xt && y is { } yt
				? _comparer.Equals(xt, yt)
				: x is null && y is null;

		/// <inheritdoc />
		public int GetHashCode(T obj)
			=> obj is { } objT ? _comparer.GetHashCode(objT) : 0;

		/// <inheritdoc />
		bool IEqualityComparer.Equals(object? x, object? y)
			=> x is T xt && y is T yt
				? _comparer.Equals(xt, yt)
				: x is null && y is null;

		/// <inheritdoc />
		int IEqualityComparer.GetHashCode(object obj)
			=> obj is T t ? _comparer.GetHashCode(t) : default;
	}

	private class UntypedToTypedEqualityComparerAdapter<T> : IEqualityComparer<T>, IEqualityComparer
	{
		private readonly IEqualityComparer _comparer;

		public UntypedToTypedEqualityComparerAdapter(IEqualityComparer comparer)
		{
			_comparer = comparer;
		}

		/// <inheritdoc />
		public bool Equals(T? x, T? y)
			=> _comparer.Equals(x, y);

		/// <inheritdoc />
		public int GetHashCode(T obj)
			=> obj is { } objT ? _comparer.GetHashCode(objT) : 0;

		/// <inheritdoc />
		bool IEqualityComparer.Equals(object? x, object? y)
			=> _comparer.Equals(x, y);

		/// <inheritdoc />
		int IEqualityComparer.GetHashCode(object? obj)
			=> obj is { } objT ? _comparer.GetHashCode(objT) : 0;
	}
}
