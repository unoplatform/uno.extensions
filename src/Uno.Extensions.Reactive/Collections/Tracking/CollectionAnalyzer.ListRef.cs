using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

internal partial class CollectionAnalyzer
{
	internal delegate T ElementAtHandler<out T>(int index);

	internal delegate int IndexOfHandler<in T>(T item, int startIndex, int count);

	/// <summary>
	/// An helper that abstract the fact that <see cref="IEqualityComparer"/> is **not** a <see cref="IEqualityComparer{T}"/>
	/// (nor the opposite)
	/// </summary>
	internal delegate bool ComparerRef<T>(T oldItem, T newItem);

	/// <summary>
	/// An helper struct that abstract contract miss-matches between different types of list
	/// (noticeably <see cref="IList"/> and <see cref="IImmutableList{T}"/>).
	/// </summary>
	protected struct ListRef<T>
	{
		public ListRef(int count, ElementAtHandler<T> elementAt, IndexOfHandler<T> indexOf)
		{
			Count = count;
			ElementAt = elementAt;
			IndexOf = indexOf;
		}

		public int Count { get; }

		public ElementAtHandler<T> ElementAt { get; }

		public IndexOfHandler<T> IndexOf { get; }
	}
}
