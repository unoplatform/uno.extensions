using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

/// <summary>
/// Implementation of the composite pattern for <see cref="ICollectionUpdaterVisitor"/>.
/// </summary>
internal class CompositeCollectionUpdaterVisitor : ICollectionUpdaterVisitor
{
	private readonly ICollectionUpdaterVisitor[] _inners;

	/// <summary>
	/// Creates a new <see cref="CompositeCollectionUpdaterVisitor"/> using a set of inner visitors.
	/// </summary>
	/// <param name="inners">The inner visitors that compose this visitor</param>
	public CompositeCollectionUpdaterVisitor(params ICollectionUpdaterVisitor[] inners)
	{
		_inners = inners;
	}

	/// <inheritdoc />
	public void AddItem(object item, ICollectionUpdateCallbacks callbacks)
	{
		foreach (var inner in _inners)
		{
			inner.AddItem(item, callbacks);
		}
	}

	/// <inheritdoc />
	public void SameItem(object original, object updated, ICollectionUpdateCallbacks callbacks)
	{
		foreach (var inner in _inners)
		{
			inner.SameItem(original, updated, callbacks);
		}
	}

	/// <inheritdoc />
	public bool ReplaceItem(object original, object updated, ICollectionUpdateCallbacks callbacks)
	{
		var handled = false;
		foreach (var inner in _inners)
		{
			var result = inner.ReplaceItem(original, updated, callbacks);

			handled |= result;
		}

		return handled;
	}

	/// <inheritdoc />
	public void RemoveItem(object item, ICollectionUpdateCallbacks callbacks)
	{
		foreach (var inner in _inners)
		{
			inner.RemoveItem(item, callbacks);
		}
	}

	/// <inheritdoc />
	public void Reset(IList oldItems, IList newItems, ICollectionUpdateCallbacks callbacks)
	{
		foreach (var inner in _inners)
		{
			inner.Reset(oldItems, newItems, callbacks);
		}
	}
}
