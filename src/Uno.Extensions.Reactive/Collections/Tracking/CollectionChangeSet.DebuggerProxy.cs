using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Collections.Tracking;

[DebuggerTypeProxy(typeof(DebuggerProxy))]
partial record CollectionChangeSet
{
	private class DebuggerProxy
	{
		public DebuggerProxy(CollectionChangeSet set)
		{
			Changes = set.ToArray();
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public IChange[] Changes { get; }
	}

	/// <inheritdoc />
	public override string ToString()
		=> string.Join(Environment.NewLine, this);
}
