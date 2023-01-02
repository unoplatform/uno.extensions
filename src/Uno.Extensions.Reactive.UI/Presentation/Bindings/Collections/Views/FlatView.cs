using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Facades.Composite;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Views
{
	/// <summary>
	/// A view which flatten the items of a <see cref="IBindableCollectionViewSource"/>
	/// <remarks>This view assume that the items of the source <see cref="IBindableCollectionViewSource"/> are of type <see cref="IObservableGroup"/>.</remarks>
	/// </summary>
	internal partial class FlatView : ICollectionView, INotifyCollectionChanged, ISupportIncrementalLoading, IDisposable, ISelectionInfo
	{
		private readonly CollectionFacet _source;
		private readonly SelectionFacet _selection;
		private readonly PaginationFacet _pagination;
		private readonly FlatCollectionChangedFacet _collectionChanged;

		public event VectorChangedEventHandler<object?> VectorChanged
		{
			add => _collectionChanged.AddVectorChangedHandler(value);
			remove => _collectionChanged.RemoveVectorChangedHandler(value);
		}
		public event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add => _collectionChanged.AddCollectionChangedHandler(value!);
			remove => _collectionChanged.RemoveCollectionChangedHandler(value!);
		}

		public FlatView(
			CollectionFacet source, 
			FlatCollectionChangedFacet collectionChangedFacet,
			SelectionFacet selectionFacet,
			PaginationFacet paginationFacet,
			IObservableVector<object?>? groups = null)
		{
			_source = source;
			_collectionChanged = collectionChangedFacet ?? throw new ArgumentNullException($"The facet '{nameof(CollectionChangedFacet)}' is required to create a '{nameof(FlatView)}'.");
			_selection = selectionFacet ?? throw new ArgumentNullException($"The facet '{nameof(SelectionFacet)}' is required to create a '{nameof(FlatView)}'.");
			_pagination = paginationFacet ?? throw new ArgumentNullException($"The facet '{nameof(PaginationFacet)}' is required to create a '{nameof(FlatView)}'.");

			CollectionGroups = groups;
		}

		public int Count
		{
			get
			{
				var count = 0;
				foreach (IObservableGroup? group in _source) // No LINQ for perf consideration
				{
					count += group?.Count ?? 0;
				}

				return count;
			}
		}
		public bool IsReadOnly => true;

		public object? this[int index]
		{
#pragma warning disable CS8766 // ICollectionView should be IList<object?>
			get => GetGroup(ref index)[index];
#pragma warning restore CS8766
			set => throw NotSupported();
		}


		public void Add(object item) => throw NotSupported();
		public void Insert(int index, object item) => throw NotSupported();
		public void RemoveAt(int index) => throw NotSupported();
		public bool Remove(object item) => throw NotSupported();
		public void Clear() => throw NotSupported();

		public bool Contains(object item)
		{
			foreach (IObservableGroup? group in _source) // No LINQ for perf consideration
			{
				if (group?.Contains(item) ?? false)
				{
					return true;
				}
			}

			return false;
		}

		public int IndexOf(object item)
		{
			var count = 0;
			foreach (IObservableGroup? group in _source)
			{
				if (group is null)
				{
					continue;
				}

				var index = group.IndexOf(item);
				if (index >= 0)
				{
					return count + index;
				}
				else
				{
					count += group.Count;
				}
			}

			return -1;
		}
		public IEnumerator<object?> GetEnumerator() => new CompositeEnumerator<object?>(_source.Head.AsList<object?>().Where(group => group is not null).Cast<IEnumerable<object?>>());
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		public void CopyTo(object[] array, int arrayIndex)
		{
			foreach (IObservableGroup? group in _source)
			{
				if (group is null)
				{
					continue;
				}

				group.CopyTo(array, arrayIndex);
				arrayIndex += group.Count;
			}
		}

		#region Selection (Single - ICollectionView.Current)
		/// <inheritdoc />
		public event CurrentChangingEventHandler CurrentChanging
		{
			add => _selection.AddCurrentChangingHandler(value);
			remove => _selection.RemoveCurrentChangingHandler(value);
		}

		/// <inheritdoc />
		public event CurrentChangedEventHandler CurrentChanged
		{
			add => _selection.AddCurrentChangedHandler(value);
			remove => _selection.RemoveCurrentChangedHandler(value);
		}

		/// <inheritdoc />
		public object? CurrentItem => _selection.CurrentItem;

		/// <inheritdoc />
		public int CurrentPosition => _selection.CurrentPosition;

		/// <inheritdoc />
		public bool IsCurrentAfterLast => _selection.IsCurrentAfterLast;

		/// <inheritdoc />
		public bool IsCurrentBeforeFirst => _selection.IsCurrentBeforeFirst;

		/// <inheritdoc />
		public bool MoveCurrentTo(object item) => _selection.MoveCurrentTo(item);

		/// <inheritdoc />
		public bool MoveCurrentToPosition(int index) => _selection.MoveCurrentToPosition(index);

		/// <inheritdoc />
		public bool MoveCurrentToFirst() => _selection.MoveCurrentToFirst();

		/// <inheritdoc />
		public bool MoveCurrentToLast() => _selection.MoveCurrentToLast();

		/// <inheritdoc />
		public bool MoveCurrentToNext() => _selection.MoveCurrentToNext();

		/// <inheritdoc />
		public bool MoveCurrentToPrevious() => _selection.MoveCurrentToPrevious();
		#endregion

		#region Selection (Multiple - ISelectionInfo)
		/// <inheritdoc />
		public void SelectRange(ItemIndexRange itemIndexRange)
			=> _selection.SelectRange(itemIndexRange);

		/// <inheritdoc />
		public void DeselectRange(ItemIndexRange itemIndexRange)
			=> _selection.DeselectRange(itemIndexRange);

		/// <inheritdoc />
		public bool IsSelected(int index)
			=> _selection.IsSelected(index);

		/// <inheritdoc />
		public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
			=> _selection.GetSelectedRanges();
		#endregion

		#region Pagination
		/// <inheritdoc />
		public bool HasMoreItems => _pagination.HasMoreItems;

		/// <inheritdoc />
		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
			=> _pagination.LoadMoreItemsAsync(count);
		#endregion

		#region Grouping
		/// <inheritdoc />
		public IObservableVector<object?>? CollectionGroups { get; }
		#endregion

		private NotSupportedException NotSupported([CallerMemberName] string? methodName = null)
			=> new(methodName + " is not supported on grouped collection.");

		private IObservableGroup GetGroup(ref int index)
		{
			foreach (IObservableGroup? group in _source)
			{
				if (group is null)
				{
					continue;
				}

				var count = group.Count;
				if (index >= count)
				{
					index -= count;
				}
				else
				{
					return group;
				}
			}

			throw new ArgumentOutOfRangeException(nameof(index));
		}

		public void Dispose()
		{
		}
	}
}
