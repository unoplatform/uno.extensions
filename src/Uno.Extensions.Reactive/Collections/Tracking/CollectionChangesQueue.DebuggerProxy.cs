using System;
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
		public INode[] Nodes { get; }
	}
}
