using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation.Collections;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets
{
	/// <summary>
	/// A collection which applies a differential logic to maintain its state
	/// <remarks>This collection is NOT THREAD SAFE </remarks>
	/// </summary>
	internal partial class CollectionFacet : INotifyCollectionChanged, IDifferentialCollection
	{
		private static readonly object[] EmptyItems = Array.Empty<object>();
		private static readonly IDifferentialCollectionNode Empty = new ResetNode(EmptyItems);

		private readonly CollectionChangedFacet _collectionChanged;
		private readonly ObservableCollectionKind _convertResetToClearAndAdd;
		private readonly CollectionUpdater.IHandler _changesHandler;
		private readonly CollectionUpdater.IHandler? _silentChangesHandler;
		private readonly Action? _onReseted;

		private IDifferentialCollectionNode _head;
		private ILogger? _log;
		private bool _logIsEnabled;
		private string? _logIdentifier;

		/// <summary>
		/// Creates a new empty collection
		/// </summary>
		/// <param name="collectionChangedFacet">The collection changed facet to use to forward the changes</param>
		/// <param name="convertResetToClearAndAdd">
		/// Configure the collection to raise a clear (reset with empty new items) and then one or some add instead of a reset with some new items.
		/// <remarks>
		///		Be aware that if you enable this behavior only for a kind of observabel collection, it means that at some points the state of the collection 
		///		won't be coherent between all the listeners of the collection depending to which event they are registered to.
		/// </remarks>
		/// </param>
		/// <param name="onReseted">
		/// An optional delegate which is invoked when the collection is reseted
		/// <remarks>This is the way to get notified when the <paramref name="convertResetToClearAndAdd"/> is enabled.</remarks>
		/// </param>
		public CollectionFacet(
			CollectionChangedFacet collectionChangedFacet, 
			ObservableCollectionKind convertResetToClearAndAdd = ObservableCollectionKind.None,
			Action? onReseted = null)
		{
			_collectionChanged = collectionChangedFacet;
			_convertResetToClearAndAdd = convertResetToClearAndAdd;
			_onReseted = onReseted;

			_head = Empty;
			_changesHandler = new CollectionChangeQueueHandler(this);
			_silentChangesHandler = new SilentCollectionChangeQueueHandler(this);
		}

		/// <summary>
		/// Creates a new collection with some items
		/// </summary>
		/// <param name="collectionChangedFacet">The collection changed facet to use to forward the changes</param>
		/// <param name="originalItems">The items that are in the collection</param>
		/// <param name="convertResetToClearAndAdd">
		/// Configure the collection to raise a clear (reset with empty new items) and then one or some add instead of a reset with some new items.
		/// <remarks>
		///		Be aware that if you enable this behavior only for a kind of observabel collection, it means that at some points the state of the collection 
		///		won't be coherent between all the listeners of the collection depending to which event they are registered to.
		/// </remarks>
		/// </param>
		/// <param name="onReseted">
		/// An optional delegate which is invoked when the collection is reseted
		/// <remarks>This is the way to get notified when the <paramref name="convertResetToClearAndAdd"/> is enabled.</remarks>
		/// </param>
		public CollectionFacet(
			CollectionChangedFacet collectionChangedFacet, 
			IList originalItems, 
			ObservableCollectionKind convertResetToClearAndAdd = ObservableCollectionKind.None,
			Action? onReseted = null)
		{
			_collectionChanged = collectionChangedFacet;
			_convertResetToClearAndAdd = convertResetToClearAndAdd;
			_onReseted = onReseted;

			_head = new ResetNode(originalItems);
			_changesHandler = new CollectionChangeQueueHandler(this);
		}

		/// <summary>
		/// Gets a boolean which indicates if currently there is any collection changed listener
		/// </summary>
		public bool HasListener => _collectionChanged.HasListener;

		/// <summary>
		/// Resets the collection with a new set of items.
		/// </summary>
		/// <param name="updated">The final collection</param>
		public void Set(IList updated)
			=> Raise(RichNotifyCollectionChangedEventArgs.Reset(new DifferentialReadOnlyList(_head), updated));

		/// <summary>
		/// Resets SILENTLY (i.e. does not raise any event for this change) the collection with a new set of items.
		/// </summary>
		/// <param name="updated">The final collection</param>
		public void SetSilently(IList updated)
			=> UpdateHead(RichNotifyCollectionChangedEventArgs.Reset(new DifferentialReadOnlyList(_head), updated));

		/// <summary>
		/// Applies a single update to the collection
		/// </summary>
		/// <param name="change">The change to apply to the collection</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// If the change is a <see cref="NotifyCollectionChangedAction.Reset"/>. 
		/// You have either to use a <see cref="RichNotifyCollectionChangedEventArgs"/> or to provide the new items using the <see cref="Set(IList)"/>.
		/// </exception>
		public void Update(NotifyCollectionChangedEventArgs change) => Raise(change);

		/// <summary>
		/// Applies a single update to the collection
		/// </summary>
		/// <param name="changes">The change to apply to the collection</param>
		public void Update(CollectionUpdater changes) => changes.DequeueChanges(_changesHandler);

		/// <summary>
		/// Applies some updates to the collection
		/// <remarks>This is equivalent to invoke multiple times the <see cref="Update(NotifyCollectionChangedEventArgs)"/>.</remarks>
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		/// If a change is a <see cref="NotifyCollectionChangedAction.Reset"/>. 
		/// You have either to use a <see cref="RichNotifyCollectionChangedEventArgs"/> or to provide the new items using the <see cref="Set(IList)"/>.
		/// </exception>
		/// <param name="changes">The changes to apply</param>
		public void Update(params NotifyCollectionChangedEventArgs[] changes)
		{
			foreach (var change in changes)
			{
				Raise(change);
			}
		}

		/// <summary>
		/// Applies a queue of changes before reseting the list to final collection.
		/// </summary>
		/// <remarks>This overload is designed to be used in conjunction with <see cref="CollectionAnalyzer"/>.</remarks>
		/// <param name="changes">The changes to apply to current state in order to reach the final collection state</param>
		/// <param name="finalCollection">The final collection</param>
		public void UpdateTo(CollectionUpdater changes, IList finalCollection)
		{
			using var operation = new BatchUpdateOperation(this, finalCollection, isSilent: false);
			operation.Update(changes);
		}

		/// <summary>
		/// Applies SILENTLY (i.e. does not raise any event for this queue) a queue of changes before reseting the list to final collection.
		/// </summary>
		/// <remarks>This overload is designed to be used in conjunction with <see cref="CollectionAnalyzer"/>.</remarks>
		/// <param name="changes">The changes to apply to current state in order to reach the final collection state</param>
		/// <param name="finalCollection">The final collection</param>
		public void UpdateSilentlyTo(CollectionUpdater changes, IList finalCollection)
		{
			using var operation = new BatchUpdateOperation(this, finalCollection, isSilent: true);
			operation.Update(changes);
		}

		/// <summary>
		/// Applies some updates before reseting the list to final collection.
		/// </summary>
		/// <remarks>This overload is designed to be used in conjunction with <see cref="CollectionAnalyzer"/>.</remarks>
		/// <param name="finalCollection">The final collection</param>
		public BatchUpdateOperation BatchUpdateTo(IList finalCollection)
			=> new(this, finalCollection);

		/// <inheritdoc />
		public IDifferentialCollectionNode Head => _head;

		#region IObservableVector
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

		/// <inheritdoc />
		public object? this[int index] => ElementAt(index);

		/// <inheritdoc />
		public int Count => _head.Count;

		/// <inheritdoc />
		public object? ElementAt(int index) => _head.ElementAt(index);
		/// <inheritdoc />
		public int IndexOf(object? value) => _head.IndexOf(value, 0, EqualityComparer<object>.Default);
		/// <inheritdoc />
		public bool Contains(object? value) => _head.IndexOf(value, 0, EqualityComparer<object>.Default) >= 0;
		/// <inheritdoc />
		public IEnumerator<object?> GetEnumerator() => new Enumerator(_head);
		/// <inheritdoc />
		public void CopyTo(Array array, int index)
		{
			using var enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				array.SetValue(enumerator.Current, index++);
			}
		}
		public void CopyTo(object?[] array, int index)
		{
			using var enumerator = GetEnumerator();
			while (enumerator.MoveNext())
			{
				array.SetValue(enumerator.Current, index++);
			}
		}
		#endregion

		private void Raise(NotifyCollectionChangedEventArgs arg)
		{
			_log = _collectionChanged.Sender.Log();
			_logIsEnabled = _log.IsEnabled(LogLevel.Information);
			_logIdentifier = _logIsEnabled ? _collectionChanged.Sender.GetType().Name : string.Empty;

			if (!_collectionChanged.HasListener)
			{
				// We don't have any event to raise, just update the _head
				UpdateHead(arg);
				_collectionChanged.PropertyChanged();
			}
#if XAMARIN // the ListView on windows does not supports event for multiple items at once (except insert at the end of the list).
			else if (_collectionChanged.VectorChanged == null)
			{
				// We can raise the event directly, so update the head and then raise the event

				if (_logIsEnabled) _log.Info($"{_logIdentifier}: Raising '{arg.Action}' @ old:{arg.OldStartingIndex}/new:{arg.NewStartingIndex} of old:{arg.OldItems?.Count ?? -1}/{arg.NewItems?.Count ?? -1} items (collection count: {_collectionChanged.Sender.Count}).");
				
				if (arg.Action == NotifyCollectionChangedAction.Reset)
				{
					RaiseReset(arg);
				}
				else
				{
					UpdateHead(arg);
					_collectionChanged.CollectionChanged?.Invoke(arg);
				}
			}
#endif
			else
			{
				// We have to ensure to not raise any event that does change multiple items at once ...

				if (_logIsEnabled) _log.Info($"{_logIdentifier}: Will raise, item per item, '{arg.Action}' @ old:{arg.OldStartingIndex}/new:{arg.NewStartingIndex} of old:{arg.OldItems?.Count ?? -1}/{arg.NewItems?.Count ?? -1} items (collection count: {_collectionChanged.Sender.Count}).");

				RaiseItemPerItem(arg);
			}
		}

		private void RaiseItemPerItem(NotifyCollectionChangedEventArgs arg)
		{
			var originalHead = _head;
			switch (arg.Action)
			{
				case NotifyCollectionChangedAction.Add when arg.NewItems!.Count > 1:
					for (var i = 0; i < arg.NewItems.Count; i++)
					{
						var index = arg.NewStartingIndex + i;
						var partialAdd = RichNotifyCollectionChangedEventArgs.Add(arg.NewItems[i], index);
						
						RaiseAdd(partialAdd, "", (i, arg.NewItems.Count));
					}
					UpdateHead(arg, originalHead);
					break;

				case NotifyCollectionChangedAction.Add:
					RaiseAdd(arg);
					break;

				case NotifyCollectionChangedAction.Move when arg.OldItems!.Count > 1:
					if (arg.NewStartingIndex > arg.OldStartingIndex)
					{
						var fromIndex = arg.OldStartingIndex;
						var toIndex = arg.NewStartingIndex + arg.NewItems!.Count - 1;

						for (var i = 0; i < arg.NewItems.Count; i++)
						{
							var change = RichNotifyCollectionChangedEventArgs.Move(arg.NewItems[i], fromIndex, toIndex);
							
							RaiseMove(change, "left to right");
						}
					}
					else
					{
						var fromIndex = arg.OldStartingIndex + arg.OldItems.Count - 1;
						var toIndex = arg.NewStartingIndex;

						for (var i = arg.NewItems!.Count - 1; i >= 0; i--)
						{
							var change = RichNotifyCollectionChangedEventArgs.Move(arg.NewItems[i], fromIndex, toIndex);

							RaiseMove(change, "right to left");
						}
					}
					UpdateHead(arg, originalHead);
					break;

				case NotifyCollectionChangedAction.Move:
					RaiseMove(arg);
					break;

				case NotifyCollectionChangedAction.Replace when arg.OldItems!.Count > 1 || arg.NewItems!.Count > 1:
					for (var i = 0; i < Math.Min(arg.NewItems!.Count, arg.OldItems.Count); i++)
					{
						RaiseReplace(RichNotifyCollectionChangedEventArgs.Replace(arg.OldItems[i], arg.NewItems[i], arg.OldStartingIndex + i), item: (i, arg.NewItems.Count));
					}

					// Add extra items
					for (var i = arg.OldItems.Count; i < arg.NewItems.Count; i++)
					{
						RaiseAdd(RichNotifyCollectionChangedEventArgs.Add(arg.NewItems[i], arg.OldStartingIndex + i), "for 'Replace'", (i, arg.NewItems.Count));
					}

					// Remove trailing items
					var removeIndex = arg.OldStartingIndex + arg.NewItems.Count;
					for (var i = arg.NewItems.Count; i < arg.OldItems.Count; i++)
					{
						RaiseRemove(RichNotifyCollectionChangedEventArgs.Remove(arg.OldItems[i], removeIndex), "for 'Replace'", (i, arg.OldItems.Count));
					}
					UpdateHead(arg, originalHead);
					break;

				case NotifyCollectionChangedAction.Replace:
					RaiseReplace(arg);
					break;

				case NotifyCollectionChangedAction.Remove when arg.OldItems!.Count > 1:
					for (var i = 0; i < arg.OldItems.Count; i++)
					{
						RaiseRemove(RichNotifyCollectionChangedEventArgs.Remove(arg.OldItems[i], arg.OldStartingIndex), "", (i, arg.OldItems.Count));
					}
					UpdateHead(arg, originalHead);
					break;

				case NotifyCollectionChangedAction.Remove:
					RaiseRemove(arg);
					break;

				case NotifyCollectionChangedAction.Reset:
					RaiseReset(arg);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(arg), $"'{arg.Action}' is not supported.");
			}
		}

		private void RaiseAdd(NotifyCollectionChangedEventArgs arg, string? logMeta = null, (int number, int of)? item = null)
		{
			UpdateHead(arg);

			if (_logIsEnabled) _log!.Info(
				$"{_logIdentifier}: Raising 'Add' {logMeta} {arg.NewItems!.Count} @ {arg.NewStartingIndex} " +
				$"({(item.HasValue ? $"[PER ITEM] item #{item.Value.number} of {item.Value.of}; " : "")}" +
				$"collection count: {_collectionChanged.Sender.Count}).");

			_collectionChanged.CollectionChanged?.Invoke(arg);
			_collectionChanged.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)arg.NewStartingIndex));
			_collectionChanged.PropertyChanged();
		}

		private void RaiseMove(NotifyCollectionChangedEventArgs arg, string? logMeta = null, (int number, int of)? item = null)
		{
			var fromIndex = arg.OldStartingIndex;
			var toIndex = arg.NewStartingIndex;

			var partialUpdate = new RemoveNode(_head, RichNotifyCollectionChangedEventArgs.Remove(null, fromIndex));

			UpdateHead(arg);

			if (_logIsEnabled) _log!.Info(
				$"{_logIdentifier}: Raising 'Move' {logMeta} {arg.NewItems!.Count} from {fromIndex} to {toIndex} " +
				$"({(item.HasValue ? $"[PER ITEM] item #{item.Value.number} of {item.Value.of}; " : "")}" +
				$"collection count: {_collectionChanged.Sender.Count}).");

			_collectionChanged.CollectionChanged?.Invoke(arg);
			if (_collectionChanged.VectorChanged != null)
			{
				var final = _head;
				_head = partialUpdate;
				_collectionChanged.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint)fromIndex));
				_head = final;

				_collectionChanged.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)toIndex));
			}
			_collectionChanged.PropertyChanged();
		}

		private void RaiseReplace(NotifyCollectionChangedEventArgs arg, string? logMeta = null, (int number, int of)? item = null)
		{
			UpdateHead(arg);

			if (_logIsEnabled) _log!.Info(
				$"{_logIdentifier}: Raising 'Replace' {logMeta} {arg.NewItems!.Count} @ {arg.NewStartingIndex} " +
				$"({(item.HasValue ? $"[PER ITEM] item #{item.Value.number} of {item.Value.of}; " : "")}" +
				$"collection count: {_collectionChanged.Sender.Count}).");

			_collectionChanged.CollectionChanged?.Invoke(arg);
			_collectionChanged.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemChanged, (uint)arg.NewStartingIndex));
			_collectionChanged.PropertyChanged();
		}

		private void RaiseRemove(NotifyCollectionChangedEventArgs arg, string? logMeta = null, (int number, int of)? item = null)
		{
			UpdateHead(arg);

			if (_logIsEnabled) _log!.Info(
				$"{_logIdentifier}: Raising 'Remove' {logMeta} {arg.OldItems!.Count} @ {arg.OldStartingIndex} " +
				$"({(item.HasValue ? $"[PER ITEM] item #{item.Value.number} of {item.Value.of}; " : "")}" +
				$"collection count: {_collectionChanged.Sender.Count}).");

			_collectionChanged.CollectionChanged?.Invoke(arg);
			_collectionChanged.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemRemoved, (uint)arg.OldStartingIndex));
			_collectionChanged.PropertyChanged();
		}
		private void RaiseReset(NotifyCollectionChangedEventArgs poor)
		{
			UpdateHead(poor);
			var arg = (RichNotifyCollectionChangedEventArgs) poor; // Type was already checked by the UpdateHead 

			if (_logIsEnabled) _log!.Info(
				$"{_logIdentifier}: Raising 'Reset' " +
				$"(collection count: {_collectionChanged.Sender.Count}).");

			var hasNewItems = arg.ResetNewItems!.Count > 0;
			var multiCollectionChanged = hasNewItems && _convertResetToClearAndAdd.HasFlag(ObservableCollectionKind.Collection);
			var multiVectorChanged = hasNewItems && _convertResetToClearAndAdd.HasFlag(ObservableCollectionKind.Vector);

			if (!multiCollectionChanged)
			{
				_collectionChanged.CollectionChanged?.Invoke(arg);
			}

			if (!multiVectorChanged)
			{
				_collectionChanged.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.Reset, 0));
			}

			if (multiCollectionChanged || multiVectorChanged)
			{
				var final = _head;

				// Set the head to empty and raise a 'Clear' (i.e. Reset with empty new items)
				_head = Empty;
				if (multiCollectionChanged)
				{
					_collectionChanged.CollectionChanged?.Invoke(RichNotifyCollectionChangedEventArgs.Reset(arg.ResetOldItems, EmptyItems));
				}
				if (multiVectorChanged)
				{
					_collectionChanged.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.Reset, 0));
				}

				// Then add the new items
				if (multiCollectionChanged && (!multiVectorChanged || _collectionChanged.VectorChanged == null))
				{
					// As we don't have to raise vector changed, we can raise a single add
					_head = final;
					_collectionChanged.CollectionChanged?.Invoke(RichNotifyCollectionChangedEventArgs.AddSome(arg.ResetNewItems, 0));
				}
				else // if (multiVectorChanged)
				{
					// We have to raise multiple add ...
					_head = Empty;
					for (var i = 0; i < arg.ResetNewItems.Count; i++)
					{
						var add = RichNotifyCollectionChangedEventArgs.Add(arg.ResetNewItems[i], i);
						_head = new AddNode(_head, add);
						if (multiCollectionChanged)
						{
							_collectionChanged.CollectionChanged?.Invoke(add);
						}
						_collectionChanged.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemInserted, (uint)i));
					}
					_head = final;
				}
			}

			_onReseted?.Invoke();
			_collectionChanged.PropertyChanged();
		}

		private void UpdateHead(NotifyCollectionChangedEventArgs change)
			=> UpdateHead(change, _head);

		private void UpdateHead(NotifyCollectionChangedEventArgs change, IDifferentialCollectionNode head)
		{
			switch (change.Action)
			{
				case NotifyCollectionChangedAction.Add:
					_head = new AddNode(head, change);
					break;

				case NotifyCollectionChangedAction.Move:
					_head = new MoveNode(head, change);
					break;

				case NotifyCollectionChangedAction.Remove:
					_head = new RemoveNode(head, change);
					break;

				case NotifyCollectionChangedAction.Replace:
					_head = new ReplaceNode(head, change);
					break;

				case NotifyCollectionChangedAction.Reset when change is RichNotifyCollectionChangedEventArgs rich:
					_head = new ResetNode(rich.ResetNewItems!);
					break;

				case NotifyCollectionChangedAction.Reset:
					throw new ArgumentOutOfRangeException(
						nameof(change), 
						"'Reset' is not supported as update on differential collection. Use the overload with the resetted list content " +
						"(or use a RichNotifyCollectionChangedEventArgs).");

				default:
					throw new ArgumentOutOfRangeException(nameof(change), $"Unknown change type '${change.Action}'.");
			}
		}

		private void ResetHead(IList updated)
		{
#if DEBUG
			var before = _head.AsList<object>();
#endif
			// When all event has been raised, we can override the _head (without any events)
			_head = new ResetNode(updated);
#if DEBUG
			var after = _head.AsList<object>();
			Debug.Assert(before.SequenceEqual(after), "There is an inconsistency between the current updated by events, and the result collection.");
#endif
		}

		private class CollectionChangeQueueHandler : CollectionUpdater.IHandler
		{
			private readonly CollectionFacet _owner;

			public CollectionChangeQueueHandler(CollectionFacet owner) => _owner = owner;
			public void Raise(RichNotifyCollectionChangedEventArgs args) => _owner.Raise(args);
			public void ApplySilently(RichNotifyCollectionChangedEventArgs args) => _owner.UpdateHead(args);
		}

		private class SilentCollectionChangeQueueHandler : CollectionUpdater.IHandler
		{
			private readonly CollectionFacet _owner;

			public SilentCollectionChangeQueueHandler(CollectionFacet owner) => _owner = owner;
			public void Raise(RichNotifyCollectionChangedEventArgs args) => _owner.UpdateHead(args);
			public void ApplySilently(RichNotifyCollectionChangedEventArgs args) => _owner.UpdateHead(args);
		}

		/// <summary>
		/// An handler to apply multiple consecutive updates on the collection in order to reset it to a given version
		/// </summary>
		public sealed class BatchUpdateOperation : IDisposable
		{
			private readonly CollectionFacet _owner;
			private readonly IList _result;
			private readonly bool _isSilent;
			private readonly CollectionUpdater.IHandler? _handler;

			internal BatchUpdateOperation(CollectionFacet owner, IList result, bool isSilent = false)
			{
				_owner = owner;
				_result = result;
				_isSilent = isSilent;
				_handler = isSilent 
					? _owner._silentChangesHandler 
					: _owner._changesHandler;
			}

			/// <summary>
			/// Apply a temporary update as a step toward the final collection
			/// </summary>
			/// <exception cref="ArgumentOutOfRangeException">
			/// If a change is a <see cref="NotifyCollectionChangedAction.Reset"/>. 
			/// You have either to use a <see cref="RichNotifyCollectionChangedEventArgs"/> or to provide the new items using the <see cref="CollectionFacet.Set(IList)"/>.
			/// </exception>
			public void Update(NotifyCollectionChangedEventArgs change) => _owner.Raise(change);

			/// <summary>
			/// Apply a temporary update as a step toward the final collection
			/// </summary>
			public void Update(CollectionUpdater changes) => changes.DequeueChanges(_handler, _isSilent);

			/// <summary>
			/// Apply the final collection (won't raise any event)
			/// </summary>
			public void Dispose()
			{
				if (_isSilent)
				{
					_owner.SetSilently(_result);
				}
				else
				{
					_owner.ResetHead(_result);
				}
			}
		}
	}
}
