using System;
using System.Linq;
using Uno.Extensions.Collections;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Data
{
	internal class BasicUpdateCounter : IUpdateContext
	{
		private readonly int _limit;

		private int _count = -1;
		private Change _current;

		public BasicUpdateCounter(VisitorType type, TrackingMode mode, int limit)
		{
			_limit = limit;
			Type = type;
			Mode = mode;
		}

		/// <inheritdoc />
		public VisitorType Type { get; }
		
		/// <inheritdoc />
		public TrackingMode Mode { get; }

		/// <inheritdoc />
		public bool HasReachedLimit => _count > _limit;

		public void NotifyAdd() => Notify(Change.Add);
		public void NotifySameItem() { }
		public void NotifyReplace() => Notify(Change.Replace);
		public void NotifyRemove() => Notify(Change.Remove);
		public void NotifyReset() => Notify(Change.Reset);

		private void Notify(Change change)
		{
			if (_current != change)
			{
				_count++;
				_current = change;
			}
		}

		private enum Change
		{
			Add,
			Replace,
			Remove,
			Reset
		}
	}
}