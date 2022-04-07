using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Extensions.Reactive.Events;

namespace Umbrella.Presentation.Feeds.Collections
{
	/// <summary>
	/// A set of **BINDABLE** properties that are exposing some internal states of the <see cref="BindableCollection"/>.
	/// </summary>
	public sealed class BindableCollectionExtendedProperties : INotifyPropertyChanged
	{
		private readonly EventManager<PropertyChangedEventHandler, PropertyChangedEventArgs> _propertyChanged;

		private bool _isLoadingMoreItems;
		private bool _hasMoreItems;

		/// <summary>
		/// Gets an observable sequence of load request made by this collection
		/// </summary>
		public event EventHandler<LoadMoreItemsRequest>? LoadMoreItemsRequested;

		/// <inheritdoc />
		public event PropertyChangedEventHandler? PropertyChanged
		{
			add => _propertyChanged.Add(value);
			remove => _propertyChanged.Remove(value);
		}

		internal BindableCollectionExtendedProperties()
		{
			// TODO: Uno - We can have only one dispatcher here, no need to support multi threads
			_propertyChanged = new EventManager<PropertyChangedEventHandler, PropertyChangedEventArgs>(this, h => h.Invoke, isCoalescable: false);
		}

		/// <summary>
		/// Gets a boolean which indicates if more items are expected for this collection
		/// </summary>
		/// <remarks>
		/// Be aware that is only an indication, depending of the source of this collection, it may be `true`
		/// but a request to <see cref="BindableCollection.LoadMoreItemsAsync(uint)"/> won't load any new items.
		/// </remarks>
		public bool HasMoreItems
		{
			get => _hasMoreItems;
			internal set
			{
				_hasMoreItems = value; 
				OnPropertyChanged();
			}
		}

		/// <summary>
		/// Gets a boolean which indicates if the collection is currently loading more items
		/// </summary>
		/// <remarks>
		/// This will be `true` only for requests made by this collection. It won't reflect the loading state
		/// of the source itself which may also decides to load more items based on some other external triggers.
		/// </remarks>
		public bool IsLoadingMoreItems
		{
			get => _isLoadingMoreItems;
			internal set
			{
				_isLoadingMoreItems = value; 
				OnPropertyChanged();
			}
		}

		internal void LoadMoreRequested(LoadMoreItemsRequest request)
			=> LoadMoreItemsRequested?.Invoke(this, request);

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
			=> _propertyChanged.Raise(new PropertyChangedEventArgs(propertyName));
	}
}
