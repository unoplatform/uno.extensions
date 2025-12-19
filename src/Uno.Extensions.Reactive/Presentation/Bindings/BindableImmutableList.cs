using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Collections;
using _Args =  Uno.Extensions.Collections.RichNotifyCollectionChangedEventArgs;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// A bindable that wraps an immutable list of sub-bindables.
/// </summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
/// <typeparam name="TBindableItem">The type of the bindable of the item.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public class BindableImmutableList<
	[DynamicallyAccessedMembers(ListFeed.TRequirements)]
	TItem,
	TBindableItem
> : BindableEnumerable<IImmutableList<TItem>, TItem, TBindableItem>, IList<TItem>
	where TBindableItem : Bindable<TItem>
	where TItem : notnull
{
	private readonly CollectionAnalyzer<TItem> _analyzer;

	/// <inheritdoc />
	public BindableImmutableList(
		BindablePropertyInfo<IImmutableList<TItem>> property,
		Func<BindablePropertyInfo<TItem>, TBindableItem> bindableFactory)
		: this(property, bindableFactory, default)
	{
	}

	/// <inheritdoc />
	internal BindableImmutableList(
		BindablePropertyInfo<IImmutableList<TItem>> property,
		Func<BindablePropertyInfo<TItem>, TBindableItem> bindableFactory,
		ItemComparer<TItem> comparer)
		: base(property, bindableFactory, BindableConfig.Default & ~BindableConfig.AutoInit)
	{
		_analyzer = new CollectionAnalyzer<TItem>(ListFeed<TItem>.GetComparer(comparer));

		// As we disabled the `AutoInit`, we make sure to init by our own.
		Initialize();
	}

	/// <inheritdoc />
	private protected override CollectionChangeSet<TItem> GetChanges(IImmutableList<TItem> previous, IImmutableList<TItem> current)
		// '_analyzer' might be null when the base.ctor subscribe to the 'property' and invokes the 'OnOwnerUpdated'
		// That's should not happen since the `AutoInit` has been disable, but for safety we fallback on ListFeed<TItem>.DefaultAnalyzer.
		// This is valid as in that case the 'previous' will be null/empty anyway.
		=> (_analyzer ?? ListFeed<TItem>.DefaultAnalyzer).GetChanges(previous ?? ImmutableList<TItem>.Empty, current ?? ImmutableList<TItem>.Empty);

	private protected override IImmutableList<TItem> Replace(IImmutableList<TItem>? items, TItem oldItem, TItem newItem)
	{
		items ??= ImmutableList<TItem>.Empty;

		if (items is ImmutableList<TItem> concrete)
		{
			return concrete.Replace(oldItem, newItem);
		}

		var index = items.IndexOf(oldItem);
		if (index >= 0)
		{
			return items.RemoveAt(index).Insert(index, newItem);
		}

		return items;
	}

	private IImmutableList<TItem> Inner => GetValue() ?? ImmutableList<TItem>.Empty;

	/// <inheritdoc cref="IList{T}" />
	public override bool IsReadOnly => false;

	/// <inheritdoc />
	public TItem this[int index]
	{
		get => GetValue()[index];
		set => SetValue(Inner.SetItem(index, value), _analyzer.GetChanges(_Args.Replace(Inner[index], value, index)));
	}

	/// <inheritdoc />
	public bool Contains(TItem item)
		=> Inner.Contains(item);

	/// <inheritdoc />
	public int IndexOf(TItem item)
		=> Inner.IndexOf(item);

	/// <inheritdoc />
	public void Add(TItem item)
		=> SetValue(Inner.Add(item), _analyzer.GetChanges(_Args.Add(item, Inner.Count)));

	/// <inheritdoc />
	public void Insert(int index, TItem item)
		=> SetValue(Inner.Insert(index, item), _analyzer.GetChanges(_Args.Add(item, index)));

	/// <inheritdoc cref="IList{T}" />
	public override void RemoveAt(int index)
	{
		var items = Inner;
		var item = Inner[index];

		SetValue(items.RemoveAt(index), _analyzer.GetChanges(_Args.Remove(item, index)));
	}

	/// <inheritdoc />
	public bool Remove(TItem item)
	{
		var items = Inner;
		var index = items.IndexOf(item);
		if (index >= 0)
		{
			SetValue(items.Remove(item), _analyzer.GetChanges(_Args.Remove(item, index)));
			return true;
		}
		else
		{
			return false;
		}
	}

	/// <inheritdoc cref="IList{T}" />
	public override void Clear()
	{
		var items = Inner;
		var empty = Inner.Clear();

		SetValue(empty, _analyzer.GetResetChange(items, empty));
	}

	/// <inheritdoc />
	public void CopyTo(TItem[] array, int arrayIndex)
		=> ((ICollection<TItem>)Inner).CopyTo(array, arrayIndex);

	/// <inheritdoc />
	IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator()
		=> Inner.GetEnumerator();
}
