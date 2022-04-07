using System;
using System.Collections;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

/// <summary>
/// Implementation of the composite pattern for <see cref="ICollectionTrackingVisitor"/>.
/// </summary>
internal class CompositeCollectionTrackingVisitor : ICollectionTrackingVisitor
{
	private readonly ICollectionTrackingVisitor[] _inners;

	/// <summary>
	/// Creates a new <see cref="CompositeCollectionTrackingVisitor"/> using a set of inner visitors.
	/// </summary>
	/// <param name="inners">The inner visitors that compose this visitor</param>
	public CompositeCollectionTrackingVisitor(params ICollectionTrackingVisitor[] inners)
	{
		_inners = inners;
	}

	/// <inheritdoc />
	public void AddItem(object item, ICollectionTrackingCallbacks callbacks)
	{
		foreach (var inner in _inners)
		{
			inner.AddItem(item, callbacks);
		}
	}

	/// <inheritdoc />
	public void SameItem(object original, object updated, ICollectionTrackingCallbacks callbacks)
	{
		foreach (var inner in _inners)
		{
			inner.SameItem(original, updated, callbacks);
		}
	}

	/// <inheritdoc />
	public bool ReplaceItem(object original, object updated, ICollectionTrackingCallbacks callbacks)
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
	public void RemoveItem(object item, ICollectionTrackingCallbacks callbacks)
	{
		foreach (var inner in _inners)
		{
			inner.RemoveItem(item, callbacks);
		}
	}

	/// <inheritdoc />
	public void Reset(IList oldItems, IList newItems, ICollectionTrackingCallbacks callbacks)
	{
		foreach (var inner in _inners)
		{
			inner.Reset(oldItems, newItems, callbacks);
		}
	}
}
