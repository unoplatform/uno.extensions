using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;
using Uno.Extensions.Reactive.Bindings.Collections.Services;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Utils;
using ISchedulersProvider = Uno.Extensions.Reactive.Dispatching.FindDispatcher;

namespace Uno.Extensions.Reactive.Bindings.Collections
{
	/// <summary>
	/// A collection which is responsible to manage the items tracking.
	/// </summary>
	internal sealed partial class BindableCollection : ICollectionView, INotifyCollectionChanged, INotifyPropertyChanged, ISelectionInfo
	{
		private readonly IBindableCollectionDataStructure _dataStructure;
		private readonly DispatcherLocal<DataLayer> _holder;

		private IObservableCollection? _current;

		/// <summary>
		/// Creates a new instance of a <see cref="BindableCollection"/>.
		/// </summary>
		/// <param name="initial">The initial items in the collection.</param>
		/// <param name="itemComparer">Comparer used to track items.</param>
		/// <param name="schedulersProvider">Schedulers provider to use to handle concurrency.</param>
		/// <param name="services">A set of services that the collection can use (cf. Remarks)</param>
		/// <param name="resetThreshold">Threshold on which the a single reset is raised instead of multiple collection changes.</param>
		/// <remarks>
		/// Currently the BindableCollection can resolve <see cref="IPaginationService"/> on the <paramref name="services"/> provider. 
		/// </remarks>
		internal static BindableCollection Create<T>(
			IObservableCollection<T>? initial = null,
			ItemComparer<T> itemComparer = default,
			ISchedulersProvider? schedulersProvider = null,
			IServiceProvider? services = null,
			int resetThreshold = DataStructure.DefaultResetThreshold)
		{
			var dataStructure = new DataStructure((ItemComparer)itemComparer)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider, services);
		}

		/// <summary>
		/// Creates a new instance of a <see cref="BindableCollection"/>, without needing to specify the element type.
		/// </summary>
		internal static BindableCollection CreateUntyped(
			IObservableCollection? initial = null,
			ItemComparer itemComparer = default,
			ISchedulersProvider? schedulersProvider = null,
			int resetThreshold = DataStructure.DefaultResetThreshold
		)
		{
			var dataStructure = new DataStructure(itemComparer)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider);
		}

		internal static BindableCollection CreateGrouped<TKey, T>(
			IObservableCollection<TKey> initial,
			ItemComparer<TKey> keyComparer,
			ItemComparer<T> itemComparer = default,
			ISchedulersProvider? schedulersProvider = null,
			int resetThreshold = DataStructure.DefaultResetThreshold)
			where TKey : IObservableGroup<T>
		{
			var dataStructure = new DataStructure((ItemComparer)keyComparer, (ItemComparer)itemComparer)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider);
		}

		internal static BindableCollection CreateGrouped<TGroup>(
			IObservableCollection initial,
			ItemComparer<TGroup> groupComparer,
			ItemComparer itemComparer,
			ISchedulersProvider? schedulersProvider = null,
			int resetThreshold = DataStructure.DefaultResetThreshold)
			where TGroup : IObservableGroup
		{
			var dataStructure = new DataStructure((ItemComparer)groupComparer, itemComparer)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider);
		}

		internal static BindableCollection CreateUntypedGrouped(
			IObservableCollection initial,
			ItemComparer groupComparer,
			ItemComparer itemComparer,
			ISchedulersProvider? schedulersProvider = null,
			int resetThreshold = DataStructure.DefaultResetThreshold)
		{
			var dataStructure = new DataStructure(groupComparer, itemComparer)
			{
				ResetThreshold = resetThreshold
			};

			return new BindableCollection(dataStructure, initial, schedulersProvider);
		}

		internal BindableCollection(
			IBindableCollectionDataStructure dataStructure,
			IObservableCollection? initial = null,
			ISchedulersProvider? schedulersProvider = null,
			IServiceProvider? services = null)
		{
			_dataStructure = dataStructure;
			_current = initial;
			_holder = new DispatcherLocal<DataLayer>(
				context => DataLayer.Create(_dataStructure.GetRoot(), _current ?? EmptyObservableCollection<object>.Instance, services, context),
				schedulersProvider,
				allowCreationFromAnotherThread: true);
		}

		/// <summary>
		/// Reset the collection of items of the collection
		/// </summary>
		/// <param name="source">The new source to use</param>
		/// <param name="changes">The changes that has been applied compared to teh previous version.</param>
		/// <param name="mode">The tracking mode to use.</param>
		internal void Switch(IObservableCollection? source, CollectionChangeSet? changes, TrackingMode mode = TrackingMode.Auto)
		{
			source ??= EmptyObservableCollection<object>.Instance;

			// Set it as current immediately so if the collection is accessed from another thread, it will start with the right version
			var original = Interlocked.Exchange(ref _current, source);
			if (object.ReferenceEquals(source, original))
			{
				return;
			}

			_holder.ForEachValue((_, value) => value.Update(source, changes, mode)); 
		}

		/// <summary>
		/// Get a direct access to the ICollectionView implementation for the current thread.
		/// </summary>
		/// <returns></returns>
		public ICollectionView GetForCurrentThread()
			=> _holder.Value.View;

		/// <summary>
		/// Get a direct access to the ICollectionView implementation for the given UI thread.
		/// </summary>
		/// <returns></returns>
		public ICollectionView GetFor(IDispatcher dispatcher)
			=> _holder.GetValue(dispatcher).View;

		#region ICollectionView
		/// <inheritdoc />
		public IEnumerator<object> GetEnumerator()
			=> GetForCurrentThread().GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> ((IEnumerable) GetForCurrentThread()).GetEnumerator();

		/// <inheritdoc />
		public void Add(object? item)
			=> GetForCurrentThread().Add(item);

		/// <inheritdoc />
		public void Clear()
			=> GetForCurrentThread().Clear();

		/// <inheritdoc />
		public bool Contains(object? item)
			=> GetForCurrentThread().Contains(item);

		/// <inheritdoc />
		public void CopyTo(object?[] array, int arrayIndex)
			=> GetForCurrentThread().CopyTo(array, arrayIndex);

		/// <inheritdoc />
		public bool Remove(object? item)
			=> GetForCurrentThread().Remove(item);

		/// <inheritdoc />
		public int Count => GetForCurrentThread().Count;

		/// <inheritdoc />
		public bool IsReadOnly => GetForCurrentThread().IsReadOnly;

		/// <inheritdoc />
		public int IndexOf(object? item)
			=> GetForCurrentThread().IndexOf(item);

		/// <inheritdoc />
		public void Insert(int index, object? item)
			=> GetForCurrentThread().Insert(index, item);

		/// <inheritdoc />
		public void RemoveAt(int index)
			=> GetForCurrentThread().RemoveAt(index);

		/// <inheritdoc />
		public object? this[int index]
		{
#pragma warning disable CS8766 // ICollectionView should be IList<object?>
			get { return GetForCurrentThread()[index]; }
#pragma warning restore CS8766
			set { GetForCurrentThread()[index] = value; }
		}

		/// <inheritdoc />
		public event VectorChangedEventHandler<object?>? VectorChanged
		{
			add => AddVectorChangedHandler(value);
			remove => RemoveVectorChangedHandler(value);
		}

		/// <inheritdoc />
		public event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add => _holder.Value.GetFacet<CollectionChangedFacet>().AddCollectionChangedHandler(value!);
			remove => _holder.Value.GetFacet<CollectionChangedFacet>().RemoveCollectionChangedHandler(value!);
		}

		public event PropertyChangedEventHandler? PropertyChanged
		{
			add => _holder.Value.GetFacet<CollectionChangedFacet>().AddPropertyChangedHandler(value!);
			remove => _holder.Value.GetFacet<CollectionChangedFacet>().RemovePropertyChangedHandler(value!);
		}

		/// <inheritdoc />
		public bool MoveCurrentTo(object item)
			=> GetForCurrentThread().MoveCurrentTo(item);

		/// <inheritdoc />
		public bool MoveCurrentToPosition(int index)
			=> GetForCurrentThread().MoveCurrentToPosition(index);

		/// <inheritdoc />
		public bool MoveCurrentToFirst()
			=> GetForCurrentThread().MoveCurrentToFirst();

		/// <inheritdoc />
		public bool MoveCurrentToLast()
			=> GetForCurrentThread().MoveCurrentToLast();

		/// <inheritdoc />
		public bool MoveCurrentToNext()
			=> GetForCurrentThread().MoveCurrentToNext();

		/// <inheritdoc />
		public bool MoveCurrentToPrevious()
			=> GetForCurrentThread().MoveCurrentToPrevious();

		/// <inheritdoc />
		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
			=> GetForCurrentThread().LoadMoreItemsAsync(count);

		/// <inheritdoc />
		public IObservableVector<object> CollectionGroups => GetForCurrentThread().CollectionGroups;

		/// <inheritdoc />
		public object? CurrentItem => GetForCurrentThread().CurrentItem;

		/// <inheritdoc />
		public int CurrentPosition => GetForCurrentThread().CurrentPosition;

		/// <inheritdoc />
		public bool HasMoreItems => GetForCurrentThread().HasMoreItems;

		/// <inheritdoc />
		public bool IsCurrentAfterLast => GetForCurrentThread().IsCurrentAfterLast;

		/// <inheritdoc />
		public bool IsCurrentBeforeFirst => GetForCurrentThread().IsCurrentBeforeFirst;

		/// <inheritdoc />
		public event CurrentChangedEventHandler? CurrentChanged
		{
			add => AddCurrentChangedHandler(value);
			remove => RemoveCurrentChangedHandler(value);
		}

		/// <inheritdoc />
		public event CurrentChangingEventHandler? CurrentChanging
		{
			add => AddCurrentChangingHandler(value);
			remove => RemoveCurrentChangingHandler(value);
		}
		#endregion

		#region ISelectionInfo
		#pragma warning disable Uno0001 // ISelectionInfo is just an interface
		public ISelectionInfo? GetSelectionForCurrentThread()
			=> _holder.Value.View as ISelectionInfo;

		/// <inheritdoc />
		public void SelectRange(ItemIndexRange itemIndexRange)
			=> GetSelectionForCurrentThread()?.SelectRange(itemIndexRange);

		/// <inheritdoc />
		public void DeselectRange(ItemIndexRange itemIndexRange)
			=> GetSelectionForCurrentThread()?.DeselectRange(itemIndexRange);

		/// <inheritdoc />
		public bool IsSelected(int index)
			=> GetSelectionForCurrentThread()?.IsSelected(index) ?? false;

		/// <inheritdoc />
		public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
			=> GetSelectionForCurrentThread()?.GetSelectedRanges() ?? Array.Empty<ItemIndexRange>();
		#pragma warning restore Uno0001
		#endregion

		internal EventRegistrationToken AddVectorChangedHandler(VectorChangedEventHandler<object?>? handler)
			=> handler is null ? default : _holder.Value.GetFacet<CollectionChangedFacet>().AddVectorChangedHandler(handler);
		internal void RemoveVectorChangedHandler(VectorChangedEventHandler<object?>? handler)
			=> _holder.Value.GetFacet<CollectionChangedFacet>().RemoveVectorChangedHandler(handler!);
#if USE_EVENT_TOKEN
		internal void RemoveVectorChangedHandler(EventRegistrationToken token)
			=> _holder.Value.GetFacet<CollectionChangedFacet>().RemoveVectorChangedHandler(token);
#endif

		internal EventRegistrationToken AddCurrentChangedHandler(CurrentChangedEventHandler? handler)
			=> _holder.Value.GetFacet<SelectionFacet>().AddCurrentChangedHandler(handler!);
		internal void RemoveCurrentChangedHandler(CurrentChangedEventHandler? handler)
			=> _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangedHandler(handler!);
#if USE_EVENT_TOKEN
		internal void RemoveCurrentChangedHandler(EventRegistrationToken token)
			=> _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangedHandler(token);
#endif

		internal EventRegistrationToken AddCurrentChangingHandler(CurrentChangingEventHandler? handler)
			=> _holder.Value.GetFacet<SelectionFacet>().AddCurrentChangingHandler(handler!);
		internal void RemoveCurrentChangingHandler(CurrentChangingEventHandler? handler)
			=> _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangingHandler(handler!);
#if USE_EVENT_TOKEN
		internal void RemoveCurrentChangingHandler(EventRegistrationToken token)
			=> _holder.Value.GetFacet<SelectionFacet>().RemoveCurrentChangingHandler(token);
#endif

		/// <summary>
		/// Gets some extended properties for this collection
		/// </summary>
		/// <remarks>Conversely to the properties of this class, properties exposed by this bag are bindable</remarks>
		public BindableCollectionExtendedProperties ExtendedProperties => _holder.Value.GetFacet<BindableCollectionExtendedProperties>();
	}
}
