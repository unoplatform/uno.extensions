using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;

namespace Uno.Extensions.Reactive.Bindings.Collections
{
	/// <summary>
	/// Null pattern implementation of <see cref="IObservableVector{T}"/>.
	/// </summary>
	internal class NullObservableVector<T> : IObservableVector<T>
	{
#pragma warning disable CS0067 // Event not raised
		/// <inhertidoc />
		public event VectorChangedEventHandler<T>? VectorChanged;
#pragma warning restore CS0067

		/// <inhertidoc />
		public int Count { get; } = 0;

		/// <inhertidoc />
		public bool IsReadOnly { get; } = true;

		/// <inhertidoc />
		public T this[int index]
		{
			get => default!;
			set => throw new NotSupportedException();
		}

		/// <inhertidoc />
		public IEnumerator<T> GetEnumerator() => new Enumerator();

		/// <inhertidoc />
		IEnumerator IEnumerable.GetEnumerator() => new Enumerator();

		/// <inhertidoc />
		public void Add(T item) => throw new NotSupportedException();
		
		/// <inhertidoc />
		public void Clear() => throw new NotSupportedException();

		/// <inhertidoc />
		public bool Contains(T item) => false;

		/// <inhertidoc />
		public void CopyTo(T[] array, int arrayIndex) { }

		/// <inhertidoc />
		public bool Remove(T item) => false;

		/// <inhertidoc />
		public int IndexOf(T item) => -1;

		/// <inhertidoc />
		public void Insert(int index, T item) => throw new NotSupportedException();

		/// <inhertidoc />
		public void RemoveAt(int index) => throw new NotSupportedException();

		private class Enumerator : IEnumerator, IEnumerator<T>
		{
			public T Current => default!;

			object? IEnumerator.Current => Current;

			public bool MoveNext() => false;

			public void Reset() { }

			public void Dispose() { }
		}
	}
}
