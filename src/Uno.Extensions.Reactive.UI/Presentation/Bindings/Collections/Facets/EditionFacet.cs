using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Windows.Foundation.Collections;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Reactive.Bindings.Collections.Services;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;

internal class EditionFacet
{
	private readonly IBindableCollectionViewSource _source;
	private readonly CollectionFacet _collection;
	private readonly IEditionService? _service;

	public EditionFacet(IBindableCollectionViewSource source, CollectionFacet collection)
	{
		_source = source;
		_collection = collection;
		_service = source.GetService(typeof(IEditionService)) as IEditionService;
	}

	public bool IsReadOnly => _service is null;

	public void SetItem(int index, object? value)
	{
		if (_service is { } svc)
		{
			var oldItem = _collection[index];
			var newItem = value;

			svc.Update(srcNode =>
			{
				// The source collection might have been updated and not yet pushed to the current thread.
				// So to avoid out-of-range exception, we have to make sure to get the effective index from the source.
				// However, we are not validating the instances of the oldItem and newItem as they should be either same instance either Equals.
				var srcIndex = srcNode.IndexOf(oldItem, 0);
				return new ReplaceNode(srcNode, oldItem, newItem, srcIndex);
			});
			_source.Update(RichNotifyCollectionChangedEventArgs.Replace(oldItem, newItem, index));
		}
	}

	public void Add(object? value)
	{
		if (_service is { } svc)
		{
			var index = _collection.Count;
			var newItem = value;

			svc.Update(srcNode =>
			{
				// The source collection might have been updated and not yet pushed to the current thread.
				// So to avoid out-of-range exception, we have to make sure to get the effective index from the source.
				var srcIndex = Math.Min(index, srcNode.Count);
				return new AddNode(srcNode, newItem!, srcIndex);
			});
			_source.Update(RichNotifyCollectionChangedEventArgs.Add(newItem, index));
		}
	}

	public void Insert(int index, object? value)
	{
		if (_service is { } svc)
		{
			var newItem = value;

			svc.Update(srcNode =>
			{
				// The source collection might have been updated and not yet pushed to the current thread.
				// So to avoid out-of-range exception, we have to make sure to get the effective index from the source.
				var srcIndex = Math.Min(index, srcNode.Count);
				return new AddNode(srcNode, newItem!, srcIndex);
			});
			_source.Update(RichNotifyCollectionChangedEventArgs.Add(newItem, index));
		}
	}

	public void RemoveAt(int index)
	{
		if (_service is { } svc)
		{
			var oldItem = _collection[index];

			svc.Update(srcNode =>
			{
				// The source collection might have been updated and not yet pushed to the current thread.
				// So to avoid out-of-range exception, we have to make sure to get the effective index from the source.
				// However, we are not validating the instances of the oldItem and newItem as they should be either same instance either Equals.
				var srcIndex = srcNode.IndexOf(oldItem, 0);
				return srcIndex < 0 ? srcNode : new RemoveNode(srcNode, oldItem, srcIndex);
			});
			_source.Update(RichNotifyCollectionChangedEventArgs.Remove(oldItem, index));
		}
	}

	public bool Remove(object? value)
	{
		if (_service is { } svc)
		{
			var index = _collection.IndexOf(value!);
			if (index < 0)
			{
				return false;
			}

			var oldItem = value;

			svc.Update(srcNode =>
			{
				// The source collection might have been updated and not yet pushed to the current thread.
				// So to avoid out-of-range exception, we have to make sure to get the effective index from the source.
				// However, we are not validating the instances of the oldItem and newItem as they should be either same instance either Equals.
				var srcIndex = srcNode.IndexOf(oldItem, 0);
				return srcIndex < 0 ? srcNode : new RemoveNode(srcNode, oldItem, srcIndex);
			});
			_source.Update(RichNotifyCollectionChangedEventArgs.Remove(oldItem, index));

			return true;
		}

		return false;
	}

	public void Clear()
	{
		if (_service is { } svc)
		{
			var oldItems = _collection.Head.AsList();
			var newItems = Array.Empty<object?>();

			svc.Update(_ => new EmptyNode());
			_source.Update(RichNotifyCollectionChangedEventArgs.Reset(oldItems, newItems));
		}
	}
}
