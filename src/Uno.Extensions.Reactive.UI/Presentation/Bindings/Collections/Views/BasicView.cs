using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Views
{
	/// <summary>
	/// A basic view on a <see cref="IBindableCollectionViewSource"/>.
	/// </summary>
	internal partial class BasicView : INotifyCollectionChanged, INotifyPropertyChanged, ICollectionView, ISupportIncrementalLoading, IDisposable, ISelectionInfo
	{
		private readonly CollectionFacet _collection;
		private readonly CollectionChangedFacet _collectionChanged;
		private readonly SelectionFacet? _selection;
		private readonly PaginationFacet? _pagination;
		private readonly EditionFacet? _edition;

		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(BasicView))]
		public BasicView(
			CollectionFacet collectionFacet,
			CollectionChangedFacet collectionChangedFacet,
			BindableCollectionExtendedProperties extendedProperties,
			SelectionFacet? selectionFacet = null,
			PaginationFacet? paginationFacet = null,
			EditionFacet? editionFacet = null)
		{
			_collection = collectionFacet;
			_collectionChanged = collectionChangedFacet;
			ExtendedProperties = extendedProperties;
			_selection = selectionFacet;
			_pagination = paginationFacet;
			_edition = editionFacet;
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
		public bool IsReadOnly => _edition?.IsReadOnly ?? false;

		public object? this[int index]
		{
			get => _collection[index];
			set => (_edition ?? throw EditionNotSupported()).SetItem(index, value);
		}

		public int IndexOf(object item) => _collection.IndexOf(item);

		public bool Contains(object item) => _collection.Contains(item);

		public void Add(object item) => (_edition ?? throw EditionNotSupported()).Add(item);

		public void Insert(int index, object item) => (_edition ?? throw EditionNotSupported()).Insert(index, item);

		public bool Remove(object item) => (_edition ?? throw EditionNotSupported()).Remove(item);

		public void RemoveAt(int index) => (_edition ?? throw EditionNotSupported()).RemoveAt(index);

		public void Clear() => (_edition ?? throw EditionNotSupported()).Clear();

		IEnumerator<object?> IEnumerable<object?>.GetEnumerator() => _collection.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _collection.GetEnumerator();

		public void CopyTo(object[] array, int arrayIndex) => _collection.CopyTo(array, arrayIndex);

		private NotSupportedException EditionNotSupported([CallerMemberName] string? method = null)
			=> new($"{method} is not supported on a read-only collection.");
		#endregion

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler? PropertyChanged
		{
			add => _collectionChanged.AddPropertyChangedHandler(value!);
			remove => _collectionChanged.RemovePropertyChangedHandler(value!);
		}
		#endregion

		#region Selection (Single - ICollectionView.Current)
		/// <inheritdoc />
		public event CurrentChangingEventHandler CurrentChanging
		{
#if USE_EVENT_TOKEN
			add => _selection?.AddCurrentChangingHandler(value) ?? default(EventRegistrationToken);
#else
			add => _selection?.AddCurrentChangingHandler(value);
#endif
			remove => _selection?.RemoveCurrentChangingHandler(value);
		}

		/// <inheritdoc />
		public event CurrentChangedEventHandler CurrentChanged
		{
#if USE_EVENT_TOKEN
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

		#region Selection (Multiple - ISelectionInfo)
		/// <inheritdoc />
		public void SelectRange(ItemIndexRange itemIndexRange)
			=> _selection?.SelectRange(itemIndexRange);

		/// <inheritdoc />
		public void DeselectRange(ItemIndexRange itemIndexRange)
			=> _selection?.DeselectRange(itemIndexRange);

		/// <inheritdoc />
		public bool IsSelected(int index)
			=> _selection?.IsSelected(index) ?? false;

		/// <inheritdoc />
		public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
			=> _selection?.GetSelectedRanges() ?? Array.Empty<ItemIndexRange>();
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

		public BindableCollectionExtendedProperties ExtendedProperties { get; }

		public void Dispose() { }
	}
}
