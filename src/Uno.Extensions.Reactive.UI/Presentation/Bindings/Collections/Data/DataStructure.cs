using System;
using System.Collections;
using System.Linq;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Collections;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data
{
	internal class DataStructure : IBindableCollectionDataStructure
	{
		public const int DefaultResetThreshold = 5;

		private readonly ItemComparer[] _comparersStructure;

		public DataStructure(params ItemComparer[] comparersStructure)
		{
			_comparersStructure = comparersStructure;
		}

		public int ResetThreshold { get; set; } = DefaultResetThreshold;

		/// <inheritdoc />
		public IBindableCollectionDataLayerStrategy GetRoot() => GetLayer(0);

		public IBindableCollectionDataLayerStrategy GetLayer(uint level)
		{
			if (level >= _comparersStructure.Length)
			{
				throw new InvalidOperationException(
					"The collection was not configured to have a such deep structure. " +
					"(Are you trying to use a grouped source but without using the dedicated constructor ?).");
			}

			var comparers = _comparersStructure[level];
			var tracker = new CollectionAnalyzer(comparers);

			if (level + 1 == _comparersStructure.Length)
			{
				return new LeafStrategy(this, tracker, isRoot: level == 0);
			}
			else
			{
				return new BranchStrategy(this, level, tracker);
			}
		}

		public BasicUpdateCounter CreateUpdateContext(VisitorType type, TrackingMode mode)
			=> new(type, mode, ResetThreshold);
	}
}
