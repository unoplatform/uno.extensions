using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

[DebuggerTypeProxy(typeof(DebuggerProxy))]
sealed partial class CollectionUpdater
{
	private class DebuggerProxy
	{
		public DebuggerProxy(CollectionUpdater queue)
		{
			Nodes = queue.GetNodes().ToArray();
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public Update[] Nodes { get; }
	}

	private IEnumerable<Update> GetNodes()
	{
		var node = _head;
		while (node != null)
		{
			yield return node;
			node = node.Next;
		}
	}

	/// <inheritdoc />
	public override string ToString()
		=> string.Join(Environment.NewLine, GetNodes());
}
