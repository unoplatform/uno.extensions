using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Reactive.Utils;

internal static class EqualityComparerExtensions
{
	public static IEqualityComparer ToEqualityComparer<T>(this IEqualityComparer<T> comparer)
		=> comparer is IEqualityComparer untyped ? untyped : new TypedToUntypedEqualityComparerAdapter<T>(comparer);

	public static IEqualityComparer<T> ToEqualityComparer<T>(this IEqualityComparer comparer)
		=> comparer is IEqualityComparer<T> typed ? typed : new UntypedToTypedEqualityComparerAdapter<T>(comparer);

	private class TypedToUntypedEqualityComparerAdapter<T> : IEqualityComparer<T>, IEqualityComparer
	{
		private readonly IEqualityComparer<T> _comparer;

		public TypedToUntypedEqualityComparerAdapter(IEqualityComparer<T> comparer)
		{
			_comparer = comparer;
		}

		/// <inheritdoc />
		public bool Equals(T x, T y)
			=> _comparer.Equals(x, y);

		/// <inheritdoc />
		public int GetHashCode(T obj)
			=> _comparer.GetHashCode(obj);

		/// <inheritdoc />
		bool IEqualityComparer.Equals(object x, object y)
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
		public bool Equals(T x, T y)
			=> _comparer.Equals(x, y);

		/// <inheritdoc />
		public int GetHashCode(T obj)
			=> _comparer.GetHashCode(obj);

		/// <inheritdoc />
		bool IEqualityComparer.Equals(object x, object y)
			=> _comparer.Equals(x, y);

		/// <inheritdoc />
		int IEqualityComparer.GetHashCode(object obj)
			=> _comparer.GetHashCode(obj);
	}
}
