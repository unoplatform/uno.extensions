using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nVentive.Umbrella.Collections;
using nVentive.Umbrella.Concurrency;
using Uno.Core.Equality;

namespace Umbrella.Feeds.Collections.Facades
{
	internal class CompositeCollectionSnapshot<T> : CompositeReadOnlyList<T>, IObservableCollectionSnapshot<T>
	{
		private readonly IObservableCollectionSnapshot<T>[] _inners;

		public CompositeCollectionSnapshot(ISchedulerInfo context, params IObservableCollectionSnapshot<T>[] inners)
			: base(inners)
		{
			SchedulingContext = context;

			_inners = inners;
		}

		/// <inheritdoc />
		public ISchedulerInfo SchedulingContext { get; }

		int IObservableCollectionSnapshot.IndexOf(object item, int startIndex, IEqualityComparer comparer)
			=> IndexOf((T) item, startIndex, comparer?.ToEqualityComparer<T>());

		/// <inheritdoc />
		public int IndexOf(T item, int startIndex, IEqualityComparer<T> comparer = null)
		{
			var offset = 0;
			foreach (var inner in _inners)
			{
				var innerCount = inner.Count;
				if (startIndex > innerCount)
				{
					startIndex -= innerCount;
				}
				else
				{
					var innerIndex = inner.IndexOf(item, startIndex, comparer);
					if (innerIndex > 0)
					{
						return innerIndex + offset;
					}
				}
				offset += innerCount;
			}

			return -1;
		}
	}
}