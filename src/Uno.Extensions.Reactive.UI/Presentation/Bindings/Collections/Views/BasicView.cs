using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Umbrella.Presentation.Feeds.Collections._BindableCollection.Facets;

#if WINUI
using CurrentChangingEventHandler = Microsoft.UI.Xaml.Data.CurrentChangingEventHandler;
using CurrentChangedEventHandler = System.EventHandler<object?>;
#elif HAS_WINDOWS_UI || HAS_UMBRELLA_UI || true
using CurrentChangingEventHandler = Windows.UI.Xaml.Data.CurrentChangingEventHandler;
using CurrentChangedEventHandler = System.EventHandler<object?>;
#else
using CurrentChangingEventHandler = System.ComponentModel.CurrentChangingEventHandler;
using CurrentChangingEventArgs = System.ComponentModel.CurrentChangingEventArgs;
using CurrentChangedEventHandler = System.EventHandler;
#endif

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Views
{
	/// <summary>
	/// A basic view on a <see cref="IBindableCollectionViewSource"/>.
	/// </summary>
	internal partial class BasicView : INotifyCollectionChanged, ICollectionView, ISupportIncrementalLoading, IDisposable
	{
		private readonly CollectionFacet _collection;
		private readonly CollectionChangedFacet _collectionChanged;
		private readonly SelectionFacet? _selection;
		private readonly PaginationFacet? _pagination;

		public BasicView(
			CollectionFacet collectionFacet,
			CollectionChangedFacet collectionChangedFacet,
			SelectionFacet? selectionFacet = null,
			PaginationFacet? paginationFacet = null)
		{
			_collection = collectionFacet;
			_collectionChanged = collectionChangedFacet;
			_selection = selectionFacet;
			_pagination = paginationFacet;
		}

#region ICollection
		public event VectorChangedEventHandler<object>? VectorChanged
		{
			add => _collectionChanged.AddVectorChangedHandler(value!);
			remove => _collectionChanged.RemoveVectorChangedHandler(value!);
		}
		public event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add => _collectionChanged.AddCollectionChangedHandler(value!);
			remove => _collectionChanged.RemoveCollectionChangedHandler(value!);
		}

		public int Count => _collection.Count;
		public bool IsReadOnly => _collection.IsReadOnly;

		public object? this[int index]
		{
			get => _collection[index];
			set => _collection[index] = value;
		}

		public void Add(object item) => _collection.Add(item);

		public void Insert(int index, object item) => _collection.Insert(index, item);

		public int IndexOf(object item) => _collection.IndexOf(item);

		public bool Contains(object item) => _collection.Contains(item);

		public bool Remove(object item) => _collection.Remove(item);

		public void RemoveAt(int index) => _collection.RemoveAt(index);

		public void Clear() => _collection.Clear();

		IEnumerator<object?> IEnumerable<object?>.GetEnumerator() => _collection.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

		public void CopyTo(object[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);
#endregion

#region Selection (Single)
		/// <inheritdoc />
		public event CurrentChangingEventHandler CurrentChanging
		{
#if WINDOWS_UWP
			add => _selection?.AddCurrentChangingHandler(value) ?? default(EventRegistrationToken);
#else
			add => _selection?.AddCurrentChangingHandler(value);
#endif
			remove => _selection?.RemoveCurrentChangingHandler(value);
		}

		/// <inheritdoc />
		public event CurrentChangedEventHandler CurrentChanged
		{
#if WINDOWS_UWP
			add => _selection?.AddCurrentChangedHandler(value) ?? default(EventRegistrationToken);
#else
			add => _selection?.AddCurrentChangedHandler(value);
#endif
			remove => _selection?.RemoveCurrentChangedHandler(value);
		}

		/// <inheritdoc />
		public object? CurrentItem => _selection?.CurrentItem ?? default;

		/// <inheritdoc />
		public int CurrentPosition => _selection?.CurrentPosition ?? -1;

		/// <inheritdoc />
		public bool IsCurrentAfterLast => _selection?.IsCurrentAfterLast ?? false;

		/// <inheritdoc />
		public bool IsCurrentBeforeFirst => _selection?.IsCurrentBeforeFirst ?? false;

		/// <inheritdoc />
		public bool MoveCurrentTo(object item) => _selection?.MoveCurrentTo(item) ?? false;

		/// <inheritdoc />
		public bool MoveCurrentToPosition(int index) => _selection?.MoveCurrentToPosition(index) ?? false;

		/// <inheritdoc />
		public bool MoveCurrentToFirst() => _selection?.MoveCurrentToFirst() ?? false;

		/// <inheritdoc />
		public bool MoveCurrentToLast() => _selection?.MoveCurrentToLast() ?? false;

		/// <inheritdoc />
		public bool MoveCurrentToNext() => _selection?.MoveCurrentToNext() ?? false;

		/// <inheritdoc />
		public bool MoveCurrentToPrevious() => _selection?.MoveCurrentToPrevious() ?? false;
#endregion

#region Pagination
		/// <inheritdoc />
		public bool HasMoreItems => _pagination?.HasMoreItems ?? false;

		/// <inheritdoc />
		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
			=> _pagination?.LoadMoreItemsAsync(count) ?? PaginationFacet.EmptyResult;
#endregion

#region Grouping
		/// <inheritdoc />
		public IObservableVector<object?>? CollectionGroups { get; }
#endregion

		public void Dispose() => _pagination?.Dispose();
	}
}
