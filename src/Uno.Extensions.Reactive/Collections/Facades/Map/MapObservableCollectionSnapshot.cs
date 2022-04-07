//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using nVentive.Umbrella.Concurrency;
//using nVentive.Umbrella.Conversion;
//using nVentive.Umbrella.Extensions;
//using Uno.Core.Equality;
//using Uno.Equality;
//using Uno.Extensions.Reactive.Utils;

//namespace nVentive.Umbrella.Collections
//{
//	internal class MapObservableCollectionSnapshot<TFrom, TTo> : IObservableCollectionSnapshot<TTo>
//		where TFrom : IKeyEquatable<TFrom>
//		where TTo : IKeyEquatable<TTo>
//	{
//		private readonly IConverter<TFrom, TTo> _converter;
//		private readonly IObservableCollectionSnapshot<TFrom> _source;
//		private readonly MapReadOnlyList<TFrom, TTo> _readOnly;

//		public MapObservableCollectionSnapshot(IConverter<TFrom, TTo> converter, IObservableCollectionSnapshot<TFrom> source)
//		{
//			_converter = converter;
//			_source = source;

//			_readOnly = new MapReadOnlyList<TFrom, TTo>(converter, source);
//		}

//		int IObservableCollectionSnapshot.IndexOf(object item, int startIndex, IEqualityComparer? comparer)
//		{
//			if (comparer == null)
//			{
//				return _source.IndexOf(_converter.ConvertBack(item), startIndex);
//			}
//			else if (comparer == KeyEqualityComparer<TTo>.Default)
//			{
//				return _source.IndexOf(_converter.ConvertBack(item), startIndex, KeyEqualityComparer<TFrom>.Default);
//			}
//			else if (comparer == EqualityComparer<TTo>.Default)
//			{
//				return _source.IndexOf(_converter.ConvertBack(item), startIndex, EqualityComparer<TFrom>.Default);
//			}
//			else
//			{
//				return _source.IndexOf(_converter.ConvertBack(item), startIndex, _converter.ToEqualityComparer(comparer.ToEqualityComparer<TTo>()));
//			}
//		}

//		/// <inheritdoc />
//		public int IndexOf(TTo item, int startIndex, IEqualityComparer<TTo>? comparer = null)
//		{
//			if (comparer == null)
//			{
//				return _source.IndexOf(_converter.ConvertBack(item), startIndex);
//			}
//			else if (comparer == KeyEqualityComparer<TTo>.Default)
//			{
//				return _source.IndexOf(_converter.ConvertBack(item), startIndex, KeyEqualityComparer<TFrom>.Default);
//			}
//			else if (comparer == EqualityComparer<TTo>.Default)
//			{
//				return _source.IndexOf(_converter.ConvertBack(item), startIndex, EqualityComparer<TFrom>.Default);
//			}
//			else
//			{
//				return _source.IndexOf(_converter.ConvertBack(item), startIndex, _converter.ToEqualityComparer(comparer));
//			}
//		}

//		#region IReadOnlyList<TTo>
//		/// <inheritdoc />
//		public IEnumerator<TTo> GetEnumerator() => _readOnly.GetEnumerator();
//		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _readOnly).GetEnumerator();

//		/// <inheritdoc />
//		public void CopyTo(Array array, int index) => _readOnly.CopyTo(array, index);

//		/// <inheritdoc />
//		public int Count => _readOnly.Count;

//		/// <inheritdoc />
//		public bool IsSynchronized => _readOnly.IsSynchronized;

//		/// <inheritdoc />
//		public object SyncRoot => _readOnly.SyncRoot;

//		/// <inheritdoc />
//		public int Add(object value) => _readOnly.Add(value);

//		/// <inheritdoc />
//		public void Clear() => _readOnly.Clear();

//		/// <inheritdoc />
//		public bool Contains(object value) => _readOnly.Contains(value);

//		/// <inheritdoc />
//		public int IndexOf(object value) => _readOnly.IndexOf(value);

//		/// <inheritdoc />
//		public void Insert(int index, object value) => _readOnly.Insert(index, value);

//		/// <inheritdoc />
//		public void Remove(object value) => _readOnly.Remove(value);

//		/// <inheritdoc />
//		public void RemoveAt(int index) => _readOnly.RemoveAt(index);

//		/// <inheritdoc />
//		public bool IsFixedSize => _readOnly.IsFixedSize;

//		/// <inheritdoc />
//		public bool IsReadOnly => _readOnly.IsReadOnly;

//		object IList.this[int index]
//		{
//			get => ((IList) _readOnly)[index];
//			set => ((IList) _readOnly)[index] = value;
//		}

//		int IReadOnlyCollection<TTo>.Count => _readOnly.Count;

//		/// <inheritdoc />
//		public TTo this[int index] => _readOnly[index];
//		#endregion
//	}
//}
