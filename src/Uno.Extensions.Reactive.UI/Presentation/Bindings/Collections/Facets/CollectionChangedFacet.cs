using System;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation.Collections;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets
{
	/// <summary>
	/// A facet for collection change management
	/// </summary>
	internal class CollectionChangedFacet
	{
		protected readonly EventRegistrationTokenTable<VectorChangedEventHandler<object?>> _normalPriorityVectorChanged = new();
		protected readonly EventRegistrationTokenTable<NotifyCollectionChangedEventHandler> _normalPriorityCollectionChanged = new();
		protected readonly EventRegistrationTokenTable<VectorChangedEventHandler<object?>> _lowPriorityVectorChanged = new();
		protected readonly EventRegistrationTokenTable<NotifyCollectionChangedEventHandler> _lowPriorityCollectionChanged = new();

		private readonly Lazy<IObservableVector<object?>> _onBehalfOf;

		public CollectionChangedFacet(Func<IObservableVector<object?>> onBehalfOf)
		{
			_onBehalfOf = new Lazy<IObservableVector<object?>>(onBehalfOf, LazyThreadSafetyMode.None);
		}

		public Action<IVectorChangedEventArgs>? VectorChanged { get; private set; }
		public Action<NotifyCollectionChangedEventArgs>? CollectionChanged { get; private set; }

		/// <summary>
		/// Gets a boolean which indicates if there is some handlers currently subscribed to events or not.
		/// </summary>
		public bool HasListener => VectorChanged != null || CollectionChanged != null;
		
		/// <summary>
		/// Gets the sender that will be used to raise the events
		/// </summary>
		public IObservableVector<object?> Sender => _onBehalfOf.Value;

		public EventRegistrationToken AddVectorChangedHandler(VectorChangedEventHandler<object?> value, bool lowPriority = false)
		{
			var token = (lowPriority ? _lowPriorityVectorChanged : _normalPriorityVectorChanged).AddEventHandler(value);
			UpdateVectorChanged();
			return token;
		}

		public void RemoveVectorChangedHandler(EventRegistrationToken value, bool lowPriority = false)
		{
			(lowPriority ? _lowPriorityVectorChanged : _normalPriorityVectorChanged).RemoveEventHandler(value);
			UpdateVectorChanged();
		}

		public void RemoveVectorChangedHandler(VectorChangedEventHandler<object?> value, bool lowPriority = false)
		{
			(lowPriority ? _lowPriorityVectorChanged : _normalPriorityVectorChanged).RemoveEventHandler(value);
			UpdateVectorChanged();
		}

		public EventRegistrationToken AddCollectionChangedHandler(NotifyCollectionChangedEventHandler value, bool lowPriority = false)
		{
			var token = (lowPriority ? _lowPriorityCollectionChanged : _normalPriorityCollectionChanged).AddEventHandler(value);
			UpdateCollectionChanged();
			return token;
		}

		public void RemoveCollectionChangedHandler(EventRegistrationToken value, bool lowPriority = false)
		{
			(lowPriority ? _lowPriorityCollectionChanged : _normalPriorityCollectionChanged).RemoveEventHandler(value);
			UpdateCollectionChanged();
		}

		public void RemoveCollectionChangedHandler(NotifyCollectionChangedEventHandler value, bool lowPriority = false)
		{
			(lowPriority ? _lowPriorityCollectionChanged : _normalPriorityCollectionChanged).RemoveEventHandler(value);
			UpdateCollectionChanged();
		}

		private void UpdateVectorChanged()
		{
			VectorChangedEventHandler<object?> vectorChanged;
			if (_normalPriorityVectorChanged.InvocationList == null)
			{
				vectorChanged = _lowPriorityVectorChanged.InvocationList;
			}
			else if (_lowPriorityCollectionChanged.InvocationList == null)
			{
				vectorChanged = _normalPriorityVectorChanged.InvocationList;
			}
			else
			{
				vectorChanged = (VectorChangedEventHandler<object?>)Delegate.Combine(_normalPriorityVectorChanged.InvocationList, _lowPriorityVectorChanged.InvocationList);
			}

			// Expose it as 'Action' and keep it 'null' in order to allow null checking to avoid creation of event args if useless.
			VectorChanged = vectorChanged is null 
				? default 
				: Raise;

			void Raise(IVectorChangedEventArgs args)
			{
				var sender = _onBehalfOf.Value;
				try
				{
					vectorChanged(sender, args);
				}
				catch (Exception e)
				{
					sender.Log().Error("Failed to notify collection changed", e);
				}
			}
		}

		private void UpdateCollectionChanged()
		{
			NotifyCollectionChangedEventHandler collectionChanged;
			if (_normalPriorityCollectionChanged.InvocationList == null)
			{
				collectionChanged = _lowPriorityCollectionChanged.InvocationList;
			}
			else if (_lowPriorityCollectionChanged.InvocationList == null)
			{
				collectionChanged = _normalPriorityCollectionChanged.InvocationList;
			}
			else
			{
				collectionChanged = (NotifyCollectionChangedEventHandler)Delegate.Combine(_normalPriorityCollectionChanged.InvocationList, _lowPriorityCollectionChanged.InvocationList);
			}

			CollectionChanged = collectionChanged is null
				? default
				: Raise;

			// Expose it as 'Action' and keep it 'null' in order to allow null checking to avoid creation of event args if useless.
			void Raise(NotifyCollectionChangedEventArgs args)
			{
				var sender = _onBehalfOf.Value;
				try
				{
					collectionChanged(sender, args);
				}
				catch (Exception e)
				{
					sender.Log().Error("Failed to notify collection changed", e);
				}
			}
		}
	}
}
