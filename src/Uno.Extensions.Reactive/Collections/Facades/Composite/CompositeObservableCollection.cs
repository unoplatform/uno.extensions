using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using nVentive.Umbrella.Collections;
using nVentive.Umbrella.Concurrency;
using Uno;
using Uno.Events;
using Uno.Extensions;

namespace Umbrella.Feeds.Collections.Facades
{
	public sealed class CompositeObservableCollection<T> : CompositeList<T>, IObservableCollection<T>
	{
		private readonly IObservableCollection<T>[] _inners;
		private readonly CompositeDisposable _extensions = new CompositeDisposable();

		private readonly EventHandlerConverter<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventHandler> _collectionChanged;
		private readonly EventHandlerConverter<NotifyCollectionReadEventHandler, NotifyCollectionReadEventHandler> _collectionRead;
		private readonly Func<object, Action<RichNotifyCollectionChangedEventArgs>, Action<RichNotifyCollectionChangedEventArgs>> _wrap;

		public CompositeObservableCollection(params IObservableCollection<T>[] inners)
			: base(inners)
		{
			_inners = inners;

			foreach (var inner in _inners)
			{
				inner.RegisterExtension(this);
			}

			Action<RichNotifyCollectionChangedEventArgs> Wrap(object sender, Action<RichNotifyCollectionChangedEventArgs> callback) => args => callback(ApplyOffset(sender, args));
			_wrap = Funcs.CreateMemoized<object, Action<RichNotifyCollectionChangedEventArgs>, Action<RichNotifyCollectionChangedEventArgs>>(Wrap);

			_collectionChanged = new EventHandlerConverter<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventHandler>(
				h => (snd, e) => h(this, ApplyOffset(snd, e)),
				h =>
				{
					foreach (var inner in _inners)
					{
						inner.CollectionChanged += h;
					}
				},
				h =>
				{
					foreach (var inner in _inners)
					{
						inner.CollectionChanged -= h;
					}
				});
			_collectionRead = new EventHandlerConverter<NotifyCollectionReadEventHandler, NotifyCollectionReadEventHandler>(
				h => (snd, e) => h(this, ApplyOffset(snd, e)),
				h =>
				{
					foreach (var inner in _inners)
					{
						inner.CollectionRead += h;
					}
				},
				h =>
				{
					foreach (var inner in _inners)
					{
						inner.CollectionRead -= h;
					}
				});
		}

		IObservableCollectionSnapshot IObservableCollection.CurrentItems => CurrentItems;
		public IObservableCollectionSnapshot<T> CurrentItems => new CompositeCollectionSnapshot<T>(null, _inners.Select(i => i.CurrentItems).ToArray());

		#region events Collection Changed/Read
		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add => _collectionChanged.Add(value);
			remove => _collectionChanged.Remove(value);
		}

		public event NotifyCollectionReadEventHandler CollectionRead
		{
			add => _collectionRead.Add(value);
			remove => _collectionRead.Remove(value);
		}

		IDisposable IObservableCollection.AddCollectionChangedHandler(ISchedulerInfo context, Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
		{
			var subscription = AddCollectionChangedHandler(context, callback, out var snapshot);
			current = snapshot;
			return subscription;
		}
		public IDisposable AddCollectionChangedHandler(ISchedulerInfo context, Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current)
		{
			var subscriptions = new CompositeDisposable(_inners.Length);
			var snapshots = new IObservableCollectionSnapshot<T>[_inners.Length];
			for (var i = 0; i < _inners.Length; i++)
			{
				var inner = _inners[i];
				inner.AddCollectionChangedHandler(context, _wrap(inner, callback), out snapshots[i]).DisposeWith(subscriptions);
			}

			current = new CompositeCollectionSnapshot<T>(context, snapshots);
			return subscriptions;
		}

		void IObservableCollection.RemoveCollectionChangedHandler(ISchedulerInfo context, Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
		{
			RemoveCollectionChangedHandler(context, callback, out var snapshot);
			current = snapshot;
		}
		public void RemoveCollectionChangedHandler(ISchedulerInfo context, Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current)
		{
			var snapshots = new IObservableCollectionSnapshot<T>[_inners.Length];
			for (var i = 0; i < _inners.Length; i++)
			{
				var inner = _inners[i];
				inner.RemoveCollectionChangedHandler(context, _wrap(inner, callback), out snapshots[i]);
			}

			current = new CompositeCollectionSnapshot<T>(context, snapshots);
		}

		private RichNotifyCollectionChangedEventArgs ApplyOffset(object sender, RichNotifyCollectionChangedEventArgs args)
		{
			var innerIndex = _inners.IndexOf(sender);
			if (innerIndex < 0)
			{
				throw new InvalidOperationException("The sender of the event is invalid");
			}

			if (args.Action == NotifyCollectionChangedAction.Reset)
			{
				// For 'Reset' with 'RichNotifyCollectionChangedEventArgs' we have to update the '[Old|New]ResetItems'

				IList[] oldItems = new IList[_inners.Length], newItems = new IList[_inners.Length];
				for (var i = 0; i < _inners.Length; i++)
				{
					if (i == innerIndex)
					{
						oldItems[i] = args.ResetOldItems;
						newItems[i] = args.ResetNewItems;
					}
					else
					{
						oldItems[i] = newItems[i] = _inners[i];
					}
				}

				return RichNotifyCollectionChangedEventArgs.Reset(
					new CompositeReadOnlyList(oldItems),
					new CompositeReadOnlyList(newItems));
			}
			else
			{
				var indexOffset = _inners.Take(innerIndex).Sum(i => i.Count);

				return args.OffsetIndexBy(indexOffset);
			}
		}

		private NotifyCollectionChangedEventArgs ApplyOffset(object sender, NotifyCollectionChangedEventArgs args)
		{
			var innerIndex = _inners.IndexOf(sender);
			if (innerIndex < 0)
			{
				throw new InvalidOperationException("The sender of the event is invalid");
			}

			var indexOffset = _inners.Take(innerIndex).Sum(i => i.Count);

			return nVentive.Umbrella.Collections.CollectionChanged.Offset(args, indexOffset);
		}

		private NotifyCollectionReadEventArgs ApplyOffset(object sender, NotifyCollectionReadEventArgs args)
		{
			var innerIndex = _inners.IndexOf(sender);
			if (innerIndex < 0)
			{
				throw new InvalidOperationException("The sender of the event is invalid");
			}

			var indexOffset = _inners.Take(innerIndex).Sum(i => i.Count);

			return nVentive.Umbrella.Collections.CollectionRead.Offset(args, indexOffset);
		}
		#endregion

		#region IObservableCollection
		/// <inheritdoc />
		public void ReplaceRange(int index, int count, IReadOnlyList<T> newItems)
		{
			var itemsInsertedCount = 0;
			for (var innerIndex = 0; innerIndex < _inners.Length; innerIndex++)
			{
				var inner = _inners[innerIndex];
				var innerCount = inner.Count;

				if (index >= innerCount)
				{
					index -= innerCount;
				}
				else
				{
					var itemsToRemoveCount = Math.Min(count, innerCount - index);
					var itemsToInsertCount = innerIndex == _inners.Length - 1
						? newItems.Count - itemsInsertedCount // its the last inner, we have to insert all the remaining items
						: itemsToRemoveCount;

					inner.ReplaceRange(index, itemsToRemoveCount, newItems.Skip(itemsInsertedCount).Take(itemsToInsertCount).ToList());

					count -= itemsToRemoveCount;
					itemsInsertedCount += itemsToInsertCount;

					if (count > 0 || itemsInsertedCount < newItems.Count)
					{
						index = 0;

						continue;
					}
					else
					{
						return;
					}
				}
			}

			throw new ArgumentOutOfRangeException(nameof(index));
		}

		/// <inheritdoc />
		bool IObservableCollection.Remove(object item) => Remove((T) item);
		#endregion

		#region Extensible disposable
		public IReadOnlyCollection<object> Extensions 
			=> _extensions.ToImmutableList<object>();

		public IDisposable RegisterExtension<TExtension>(TExtension extension)
			where TExtension : class, IDisposable
			=> _extensions.DisposableAdd(extension);

		public void Dispose()
			=> _extensions.Dispose();
		#endregion
	}
}
