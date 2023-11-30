using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// A bindable that wraps a sequence of sub-bindables.
/// </summary>
/// <typeparam name="TCollection">The type of the model collection.</typeparam>
/// <typeparam name="TItem">The type of the items.</typeparam>
/// <typeparam name="TBindableItem">The type of the bindable of the item.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class BindableEnumerable<TCollection, TItem, TBindableItem> : Bindable<TCollection>, IEnumerable<TBindableItem>, INotifyCollectionChanged, IList
	where TCollection : IEnumerable<TItem>
	where TBindableItem : Bindable<TItem>
	where TItem : notnull
{
	/// <inheritdoc />
	public event NotifyCollectionChangedEventHandler? CollectionChanged;

	private readonly List<Entry> _entries = new();
	private readonly List<TBindableItem> _bindables = new(); // Shadow copy of _entries.Select(entry => entry.Bindable) for fast read access

	private readonly BindablePropertyInfo<TCollection> _listProperty;
	private readonly Func<BindablePropertyInfo<TItem>, TBindableItem> _bindableFactory;

	private Visitor? _visitor;

	/// <summary>
	/// Creates a new instance
	/// </summary>
	/// <param name="property">Info of the property that is backed by this instance.</param>
	/// <param name="bindableFactory">The factory to create an instance of <typeparamref name="TBindableItem"/>.</param>
	/// <param name="config">Advanced configuration of the base bindable.</param>
	private protected BindableEnumerable(
		BindablePropertyInfo<TCollection> property,
		Func<BindablePropertyInfo<TItem>, TBindableItem> bindableFactory,
		BindableConfig config)
		: base(property, config & ~BindableConfig.AutoInit)
	{
		_listProperty = property;
		_bindableFactory = bindableFactory;

		// As we disabled auto-init on parent, if sub-classes kept it enabled, we have to invoke it by our own.
		if (config.HasFlag(BindableConfig.AutoInit))
		{
			Initialize();
		}
	}

	private protected abstract CollectionChangeSet<TItem> GetChanges(TCollection previous, TCollection current);

	private protected abstract TCollection Replace(TCollection? items, TItem oldItem, TItem newItem);

	private protected override void UpdateSubProperties(TCollection previous, TCollection current, IChangeSet? changes)
	{
		var collectionChanges = (changes as CollectionChangeSet<TItem> ?? GetChanges(previous, current));
		base.UpdateSubProperties(previous, current, collectionChanges);
		collectionChanges.Visit(_visitor ??= new(this));
	}

	#region IList (read-only)
	/// <inheritdoc />
	public int Count => _bindables.Count;

	/// <inheritdoc />
	public object SyncRoot => _bindables;

	/// <inheritdoc />
	public bool IsSynchronized => false;

	/// <inheritdoc />
	public virtual bool IsReadOnly => true;

	/// <inheritdoc />
	public bool IsFixedSize => false;

	/// <inheritdoc />
	object? IList.this[int index]
	{
		get => _bindables[index];
		set => throw NotSupported();
	}

	/// <inheritdoc />
	public bool Contains(object? value)
		=> _bindables.Contains(value);

	/// <inheritdoc />
	public int IndexOf(object? value)
		=> value is TBindableItem item ? _bindables.IndexOf(item) : -1;

	/// <inheritdoc />
	public int Add(object? value)
		=> throw NotSupported();

	/// <inheritdoc />
	public void Insert(int index, object? value)
		=> throw NotSupported();

	/// <inheritdoc />
	public virtual void RemoveAt(int index)
		=> throw NotSupported();

	/// <inheritdoc />
	public void Remove(object? value)
		=> throw NotSupported();

	/// <inheritdoc />
	public virtual void Clear()
		=> throw NotSupported();

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
		=> ((IList)_bindables).CopyTo(array, index);

	/// <inheritdoc />
	IEnumerator<TBindableItem> IEnumerable<TBindableItem>.GetEnumerator()
		=> _bindables.GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		// For untyped enumerator, which is usually used by the view, we prefer to enumerate the bindables to allow 2-way bindings on items.
		=> _bindables.GetEnumerator();

	private NotSupportedException NotSupported([CallerMemberName] string caller = "")
		=> new(caller + " is not supported on a read-only list."); 
	#endregion

	private class Entry
	{
		private readonly BindableEnumerable<TCollection, TItem, TBindableItem> _owner;

		private Action<TItem>? _onUpdated;

		public Entry(BindableEnumerable<TCollection, TItem, TBindableItem> owner, TItem entity, int index)
		{
			_owner = owner;
			Current = entity;
			CurrentIndex = index;

			Bindable = _owner._bindableFactory(new BindablePropertyInfo<TItem>(
				_owner,
				"Item",
				getter: (_owner.Select(_ => Current), RegisterParentUpdated),
				setter: OnItemUpdated));

			void RegisterParentUpdated(Action<TItem> onUpdated)
			{
				_onUpdated = onUpdated;
				onUpdated(Current);
			}

			async ValueTask OnItemUpdated(Func<TItem, TItem> updater, bool isLeafPropertyChanged, CancellationToken ct)
			{
				var previous = Current;
				var updated = updater(previous);

				Current = updated;

				if (isLeafPropertyChanged)
				{
					_owner.RaisePropertyChanged($"Item[{CurrentIndex}]");
				}

				await _owner._listProperty.Update(list => _owner.Replace(list, previous, updated), false, ct).ConfigureAwait(false);
			}
		}

		public void Update(TItem item, int index)
		{
			Current = item;
			CurrentIndex = index;
			_onUpdated?.Invoke(item);
		}

		public TItem Current { get; private set; }

		public int CurrentIndex { get; private set; }

		public TBindableItem Bindable { get; }
	}

	private class Visitor : CollectionChangeSetVisitorBase<TItem>
	{
		private readonly BindableEnumerable<TCollection, TItem, TBindableItem> _owner;

		public Visitor(BindableEnumerable<TCollection, TItem, TBindableItem> owner)
		{
			_owner = owner;
		}

		/// <inheritdoc />
		public override void Add(IReadOnlyList<TItem> items, int index)
		{
			var count = items.Count;
			var addedBindables = new TBindableItem[count];
			for (var i = 0; i < count; i++)
			{
				var item = items[i];
				var itemIndex = index + i;
				var entry = new Entry(_owner, item, itemIndex);

				addedBindables[i] = entry.Bindable;
				_owner._entries.Insert(itemIndex, entry);
			}
			_owner._bindables.InsertRange(index, addedBindables);

			_owner.CollectionChanged?.Invoke(_owner, RichNotifyCollectionChangedEventArgs.AddSome((IList)addedBindables, index));
		}

		/// <inheritdoc />
		public override void Remove(IReadOnlyList<TItem> items, int index)
		{
			var count = items.Count;
			var removedBindables = _owner._bindables.GetRange(index, count);

			_owner._entries.RemoveRange(index, count);
			_owner._bindables.RemoveRange(index, count);

			_owner.CollectionChanged?.Invoke(_owner, RichNotifyCollectionChangedEventArgs.RemoveSome((IList)removedBindables, index));
		}

		/// <inheritdoc />
		public override void Same(IReadOnlyList<TItem> original, IReadOnlyList<TItem> updated, int index)
		{
			// Nothing to do if items are considered as equals
			// Note: If object are mutable, this will break ... but in that case the EqualityComparer provided to the CollectionAnalyzer should have 
		}

		/// <inheritdoc />
		protected override void ReplaceItem(TItem original, TItem updated, int index)
		{
			_owner._entries[index].Update(updated, index);
		}

		/// <inheritdoc />
		public override void Move(IReadOnlyList<TItem> items, int fromIndex, int toIndex)
		{
			var count = items.Count;

			var movedEntries = _owner._entries.GetRange(fromIndex, count);
			_owner._entries.RemoveRange(fromIndex, count);
			_owner._entries.InsertRange(toIndex, movedEntries);

			var movedBindables = _owner._bindables.GetRange(fromIndex, count);
			_owner._bindables.RemoveRange(fromIndex, count);
			_owner._bindables.InsertRange(toIndex, movedBindables);

			_owner.CollectionChanged?.Invoke(_owner, RichNotifyCollectionChangedEventArgs.MoveSome((IList)movedBindables, fromIndex, toIndex));
		}
	}
}
