using System;
using System.Linq;

namespace Uno.Extensions.Collections.Tracking;

partial class CollectionAnalyzer
{
	private sealed class _Same : EntityChange
	{
		/// <inheritdoc />
		public _Same(int at, int indexOffset)
			: base(at, indexOffset)
		{
		}

		/// <inheritdoc />
		public override RichNotifyCollectionChangedEventArgs? ToEvent()
			=> null;

		/// <inheritdoc />
		protected override CollectionUpdater.Update ToUpdaterCore(ICollectionUpdaterVisitor visitor)
		{
			var callback = new CollectionUpdater.Update(ToEvent());

			for (var i = 0; i < _oldItems.Count; i++)
			{
				visitor.SameItem(_oldItems[i], _newItems[i], callback);
			}

			return callback;
		}

		/// <inheritdoc />
		public override string ToString()
			=> $"Keep {_oldItems.Count} items at {Starts} (Same instance that may require a deep diff)";
	}
}
