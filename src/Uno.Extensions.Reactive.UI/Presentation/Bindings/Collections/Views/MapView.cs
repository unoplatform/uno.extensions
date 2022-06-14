#if WINDOWS
#define NEEDS_OUT_OF_RANGE_HACK
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Facades.Map;
using Uno.Extensions.Conversion;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Views
{
	/// <summary>
	/// A view on a <see cref="IBindableCollectionViewSource"/> which map each item into another type.
	/// </summary>
	internal class MapView : IObservableVector<object?>, INotifyCollectionChanged
	{
		private readonly CollectionFacet _source;
		private readonly CollectionChangedFacet _sourceChange;
		private readonly IConverter<object?, object?> _converter;

#if NEEDS_OUT_OF_RANGE_HACK
		/*
		 * About the "WinRT out of range hack"
		 *
		 * As of 2017-12-21 using an app targeting SDK 15063 on Windows 16299, pagination over grouped collection is not supported.
		 *
		 * When the view request the '[ISupportIncrementialLoading/ICollectionView].LoadMoreItems(uint count)', if the items are not loaded
		 * faster than the view needs them (ie. as soon as we are making an API call to get them ...), the view will try to get
		 * the group at index == Count, which is obviously out of range.
		 * 
		 * The issue is that not only it tries to get an item out of range, but it will check itself if this index is out of range:
		 * https://github.com/Microsoft/referencesource/blob/master/mscorlib/system/runtime/interopservices/windowsruntime/listtovectoradapter.cs
		 * at line 42, in the 'GetAt' method, it validates the range using 'EnsureIndexInt32(index, _this.Count);'
		 *
		 * This will crash the app without any possibility to catch the exception.
		 *
		 * The hack is, if the app is getting the 'Count' in the 'GetAt' method we will return 'Count + 1' so the validation will succeed.
		 * Then in the 'this[index]', if the index is out of range, we will return a 'NullCollectionViewGroup' to let the view believe that
		 * it is doing a valid operation.
		 *
		 * Note: As the detection of the 'caller' is really expensive, it cause huge performance issue. So this solution is acceptable
		 *       only for debug purpose! We also limit this check to the strict minimum by enabling it only if the last queried index is near
		 *		 the end of the collection.
		 */

		private readonly bool _enableWinRtOutOfRangeHack;
		private int _lastQueriedIndex;
#endif

		public event VectorChangedEventHandler<object?> VectorChanged
		{
			add => _sourceChange.AddVectorChangedHandler(value);
			remove => _sourceChange.RemoveVectorChangedHandler(value);
		}

		public event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add => _sourceChange.AddCollectionChangedHandler(value!);
			remove => _sourceChange.RemoveCollectionChangedHandler(value!);
		}

		public MapView(
			CollectionFacet source,
			CollectionChangedFacet sourceChange,
			IConverter<object?, object?> converter
#if NEEDS_OUT_OF_RANGE_HACK
			, bool enableWinRTOutOfRangeHack = true)
		{
			_enableWinRtOutOfRangeHack = enableWinRTOutOfRangeHack;
#else
		) {
#endif
			_source = source;
			_sourceChange = sourceChange;
			_converter = converter;
		}

		/// <inheritdoc />
		public int Count
		{
			get
			{
				var count = _source.Count;

#if NEEDS_OUT_OF_RANGE_HACK
				if (_enableWinRtOutOfRangeHack && _lastQueriedIndex >= count - 2)
				{
					var caller = Environment.StackTrace.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).Last();
					if (caller.Contains("System.Runtime.InteropServices.WindowsRuntime.ListToVectorAdapter.GetAt[T](UInt32 index)"))
					{
						return count + 1;
					}
				}
#endif

				return count;
			}
		}

		/// <inheritdoc />
		public bool IsReadOnly => true;

		/// <inheritdoc />
		public object? this[int index]
		{
			get
			{
#if NEEDS_OUT_OF_RANGE_HACK
				_lastQueriedIndex = index;
				if (index >= _source.Count)
				{
					return new NullCollectionViewGroup();
				}
#endif

				return _converter.Convert(_source[index]);
			}
			set => throw NotSupported();
		}

		/// <inheritdoc />
		public void Add(object? item) => throw NotSupported();
		/// <inheritdoc />
		public void Insert(int index, object? item) => throw NotSupported();
		/// <inheritdoc />
		public void RemoveAt(int index) => throw NotSupported();
		/// <inheritdoc />
		public bool Remove(object? item) => throw NotSupported();
		/// <inheritdoc />
		public void Clear() => throw NotSupported();

		/// <inheritdoc />
		public bool Contains(object? item) => _source.Contains(_converter.ConvertBack(item));
		/// <inheritdoc />
		public int IndexOf(object? item) => _source.IndexOf(_converter.ConvertBack(item));
		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator() => new MapEnumerator<object?, object?>(_converter, _source.GetEnumerator());
		/// <inheritdoc />
		public IEnumerator<object?> GetEnumerator() => new MapEnumerator<object?, object?>(_converter, _source.GetEnumerator());
		/// <inheritdoc />
		public void CopyTo(object?[] array, int arrayIndex) => _converter.ArrayCopy(_source, array, arrayIndex);

		private NotSupportedException NotSupported([CallerMemberName] string? methodName = null)
			=> new(methodName + " is not supported on this collection.");
	}
}
