using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

sealed partial class CollectionChangesQueue
{
	private class DebuggerProxy
	{
		public DebuggerProxy(CollectionChangesQueue queue)
		{
			Nodes = queue.GetNodes().ToArray();
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public Node[] Nodes { get; }
	}

	private IEnumerable<Node> GetNodes()
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
