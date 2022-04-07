//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Linq;
//using nVentive.Umbrella.Conversion;
//using nVentive.Umbrella.Extensions;
//using Uno.Events;

//namespace nVentive.Umbrella.Collections
//{
//	/// <summary>
//	/// A sparsed facade over an <see cref="IObservableCollection{TTo}"/> which ensure the dynamic projection of items using an <see cref="IConverter{TFrom,TTo}"/>.
//	/// </summary>
//	/// <typeparam name="TFrom">Type of items in the inner source collection.</typeparam>
//	/// <typeparam name="TTo">Type of items of this collection.</typeparam>
//	public sealed class MapObservableCollection<TFrom, TTo> : IObservableCollection<TTo>
//		where TFrom : class, IKeyEquatable<TFrom>
//		where TTo : class, IKeyEquatable<TTo>
//	{
//		private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
//		private readonly CompositeDisposable _extensions = new CompositeDisposable();

//		private readonly IObservableCollection<TFrom> _source;
//		private readonly IConverter<TFrom, TTo> _converter;
//		private readonly ICachedConverter<TFrom, TTo>? _cachedConverter;

//		/// <summary>
//		/// Creates an instance by providing conversion delegate.
//		/// <remarks>This won't eagerload the items, so conversion will be achieved on consumer thread, which may be the UIThread.</remarks>
//		/// </summary>
//		/// <param name="source">The source collection which is hidden by this facade</param>
//		/// <param name="convert">Used for read operations</param>
//		/// <param name="convertBack">Used for write operations, or read operations by item (e.g. <see cref="Remove(TTo)"/>)</param>
//		public MapObservableCollection(
//			IObservableCollection<TFrom> source,
//			Func<TFrom, TTo> convert,
//			Func<TTo, TFrom> convertBack)
//			: this(source, new LockedMemoizedConverter<TFrom, TTo>(convert, convertBack), null, eagerInitCache: false)
//		{
//		}

//		/// <summary>
//		/// Creates an instance by providing an <see cref="IConverter{TFrom,TTo}"/>.
//		/// <remarks>This won't eagerload the items, so conversion will be achieved on consumer thread, which may be the UIThread.</remarks>
//		/// </summary>
//		/// <param name="source">The source collection which is hidden by this facade</param>
//		/// <param name="converter">The converter to use to convert items in source</param>
//		public MapObservableCollection(
//			IObservableCollection<TFrom> source,
//			IConverter<TFrom, TTo> converter)
//			: this(source, converter, null, eagerInitCache: false)
//		{
//		}

//		/// <summary>
//		/// Creates an instance by providing an <see cref="IConverter{TFrom,TTo}"/>.
//		/// </summary>
//		/// <param name="source">The source collection which is hidden by this facade</param>
//		/// <param name="converter">The cached converter to use to convert items in source</param>
//		/// <param name="eagerLoadAddedItems">
//		/// Determines if the items should be eager loaded in the converter as soon as they are added in the underlying source.
//		/// <remarks>
//		/// This ensure that the conversion is achieved on the thread which add the item into the underlying collection instead of the consumer thread.
//		/// If the collection can be used from the UIThread, you should always enable that in order to sure to prepare items from a background thread.
//		/// Items already presents in the collection will be eager loaded immediately.
//		/// </remarks>
//		/// </param>
//		public MapObservableCollection(
//			IObservableCollection<TFrom> source,
//			ICachedConverter<TFrom, TTo> converter,
//			bool eagerLoadAddedItems = true)
//			: this(source, converter, converter, eagerLoadAddedItems)
//		{
//		}

//		private MapObservableCollection(
//			IObservableCollection<TFrom> source,
//			IConverter<TFrom, TTo> converter,
//			ICachedConverter<TFrom, TTo>? cachedConverter,
//			bool eagerInitCache)
//		{
//			_source = source ?? throw new ArgumentNullException(nameof(source));
//			_converter = converter ?? throw new ArgumentNullException(nameof(converter));
//			_cachedConverter = cachedConverter;

//			_collectionChanged = new EventHandlerConverter<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventHandler>(
//				h => (snd, e) => h(this, Collections.CollectionChanged.Convert(e, _converter)),
//				h => _source.CollectionChanged += h,
//				h => _source.CollectionChanged -= h);
//			_collectionRead = new EventHandlerConverter<NotifyCollectionReadEventHandler, NotifyCollectionReadEventHandler>(
//				h => (snd, e) => h(this, Collections.CollectionRead.Convert(e, _converter)),
//				h => _source.CollectionRead += h,
//				h => _source.CollectionRead -= h);
			
//			Action<RichNotifyCollectionChangedEventArgs> ToSourceCallback(Action<RichNotifyCollectionChangedEventArgs> c) => args => c(args.ConvertItems(_converter));
//			_convert = Funcs.CreateMemoized<Action<RichNotifyCollectionChangedEventArgs>, Action<RichNotifyCollectionChangedEventArgs>>(ToSourceCallback);

//			source.RegisterExtension(this);

//			if (eagerInitCache)
//			{
//				BeginEagerCaching();
//			}
//		}

//		private void BeginEagerCaching()
//		{
//			if (_cachedConverter == null)
//			{
//				throw new InvalidOperationException("The provided converter does not supports caching");
//			}

//			_source.AddCollectionChangedHandler(UpdateCache, out var snapshot).DisposeWith(_subscriptions);
//			foreach (var item in snapshot)
//			{
//				_cachedConverter.Init(item);
//			}
//		}

//		private void UpdateCache(RichNotifyCollectionChangedEventArgs args)
//		{
//			switch (args.Action)
//			{
//				case NotifyCollectionChangedAction.Add:
//				case NotifyCollectionChangedAction.Move:
//				case NotifyCollectionChangedAction.Replace:
//				case NotifyCollectionChangedAction.Remove:
//					if (args.OldItems != null)
//					{
//						foreach (TFrom item in args.OldItems)
//						{
//							_cachedConverter!.Release(item);
//						}
//					}
//					if (args.NewItems != null)
//					{
//						foreach (TFrom item in args.NewItems)
//						{
//							_cachedConverter!.Init(item);
//						}
//					}
//					break;

//				case NotifyCollectionChangedAction.Reset:
//					_cachedConverter!.ReleaseAll();
//					foreach (TFrom item in args.ResetNewItems!)
//					{
//						_cachedConverter.Init(item);
//					}
//					break;
//			}
//		}

//		#region List / IList<>
//		object IList.this[int index]
//		{
//			get => _converter.Convert(((IList)_source)[index]);
//			set => ((IList)_source)[index] = _converter.ConvertBack((TTo)value);
//		}

//		/// <inheritdoc />
//		public TTo this[int index]
//		{
//			get => _converter.Convert(((IList<TFrom>)_source)[index]);
//			set => ((IList<TFrom>)_source)[index] = _converter.ConvertBack(value);
//		}
		
//		/// <inheritdoc />
//		public bool IsReadOnly => ((IList)_source).IsReadOnly;
		
//		/// <inheritdoc />
//		public bool IsFixedSize => _source.IsFixedSize;

//		/// <inheritdoc />
//		public int Count => ((ICollection)_source).Count;

//		/// <inheritdoc />
//		public object SyncRoot => _source.SyncRoot;

//		/// <inheritdoc />
//		public bool IsSynchronized => _source.IsSynchronized;

//		#region Write
//		/// <inheritdoc />
//		public void Add(TTo item) => _source.Add(_converter.ConvertBack(item));
//		int IList.Add(object value) => ((IList)_source).Add(_converter.ConvertBack(value));

//		/// <inheritdoc />

//		public void AddRange(IReadOnlyList<TTo> items) => _source.AddRange(new MapReadOnlyList<TTo,TFrom>(_converter.Inverse(), (IList)items));

//		/// <inheritdoc />
//		public void ReplaceRange(int index, int count, IReadOnlyList<TTo> newItems) => _source.ReplaceRange(index, count, new MapReadOnlyList<TTo, TFrom>(_converter.Inverse(), (IList) newItems));

//		/// <inheritdoc />
//		public void Insert(int index, TTo item) => _source.Insert(index, _converter.ConvertBack(item));
//		void IList.Insert(int index, object value) => _source.Insert(index, _converter.ConvertBack(value));

//		/// <inheritdoc />
//		public bool Remove(TTo item) => _source.Remove(_converter.ConvertBack(item));
//		void IList.Remove(object value) => _source.Remove(_converter.ConvertBack(value));
//		bool IObservableCollection.Remove(object value) => _source.Remove(_converter.ConvertBack(value));

//		/// <inheritdoc />
//		public void RemoveAt(int index) => _source.RemoveAt(index);
		
//		/// <inheritdoc />
//		public void Clear() => _source.Clear();
//		#endregion

//		#region Read
//		/// <inheritdoc />
//		public bool Contains(TTo item) => _source.Contains(_converter.ConvertBack(item));

//		bool IList.Contains(object value) => _source.Contains(_converter.ConvertBack((TTo)value));

//		/// <inheritdoc />
//		public int IndexOf(TTo item) => _source.IndexOf(_converter.ConvertBack(item));
//		public int IndexOf(object value) => _source.IndexOf(_converter.ConvertBack((TTo)value));

//		/// <inheritdoc />
//		public IEnumerator<TTo> GetEnumerator() => new MapEnumerator<TFrom, TTo>(_converter, _source.GetEnumerator());
//		IEnumerator IEnumerable.GetEnumerator() => new MapEnumerator<TFrom, TTo>(_converter, _source.GetEnumerator());

//		/// <inheritdoc />
//		public void CopyTo(TTo[] array, int index) => _converter.ArrayCopy(_source, array, index);

//		void ICollection.CopyTo(Array array, int index) => _converter.ArrayCopy(_source, array, index);
//		#endregion
//		#endregion

//		#region INotifyCollection[Read/Changed] events
//		private readonly EventHandlerConverter<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventHandler> _collectionChanged;
//		/// <inheritdoc />
//		public event NotifyCollectionChangedEventHandler CollectionChanged
//		{
//			add => _collectionChanged.Add(value);
//			remove => _collectionChanged.Remove(value);
//		}

//		private readonly Func<Action<RichNotifyCollectionChangedEventArgs>, Action<RichNotifyCollectionChangedEventArgs>> _convert;
//		/// <inheritdoc />
//		public IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
//		{
//			var subscription = _source.AddCollectionChangedHandler(_convert(callback), out var currentSource);
//			current = new MapObservableCollectionSnapshot<TFrom, TTo>(_converter, currentSource);
//			return subscription;
//		}

//		/// <inheritdoc />
//		public IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<TTo> current)
//		{
//			var subscription = _source.AddCollectionChangedHandler(_convert(callback), out var currentSource);
//			current = new MapObservableCollectionSnapshot<TFrom, TTo>(_converter, currentSource);
//			return subscription;
//		}

//		/// <inheritdoc />
//		public void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
//		{
//			_source.AddCollectionChangedHandler(_convert(callback), out var currentSource);
//			current = new MapObservableCollectionSnapshot<TFrom, TTo>(_converter, currentSource);
//		}

//		/// <inheritdoc />
//		public void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<TTo> current)
//		{
//			_source.AddCollectionChangedHandler(_convert(callback), out var currentSource);
//			current = new MapObservableCollectionSnapshot<TFrom, TTo>(_converter, currentSource);
//		}
//		#endregion

//		///// <inheritdoc />
//		//public IReadOnlyCollection<object> Extensions => _extensions
//		//	.Concat(_source.Extensions) // Even if we manage our own set of extensions, we should keep the extensions of the _source discoverable
//		//	.ToImmutableArray();

//		///// <inheritdoc />
//		//public IDisposable RegisterExtension<TExtension>(TExtension extension)
//		//	where TExtension : class, IDisposable
//		//{
//		//	// Note: As this class is a facade which as its own lifetime, we need to keep trace of our own extensions independently of the _source.

//		//	_extensions.Add(extension);
//		//	return Disposable.Create(() => _extensions.Remove(extension));
//		//}

//		/// <inheritdoc />
//		public void Dispose()
//		{
//			// Note: We are a facade over the source, the source may have been used wiht some other facade,
//			//       we should not propagate the dispose to it.

//			_cachedConverter?.ReleaseAll();
//			_extensions.Dispose();
//			_subscriptions.Dispose();
//		}
//	}
//}
