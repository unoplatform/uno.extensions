using System;
using System.Collections.Generic;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionTracker
{
	private interface IChange
	{
		IChange? Next { get; set; }
	}

	private class Head : IChange
	{
		public IChange? Next { get; set; }
	}

	internal abstract class ChangeBase : IChange
	{
		public ChangeBase(int at)
		{
			Starts = at;
			Ends = at;
		}

		/// <summary>
		/// Index at which this change occurs
		/// </summary>
		public int Starts { get; }
			
		/// <summary>
		/// The NEXT index after this changes
		/// </summary>
		public int Ends { get; protected set; }

		/// <summary>
		/// Gets or sets the next node of the linked list
		/// </summary>
		public ChangeBase? Next { get; set; }

		#region IChange
		IChange? IChange.Next
		{
			get => Next;
			set => Next = value as ChangeBase; // There is only the 'Head' which is not a 'ChangeBase', and obviously a 'Head' should not be set as 'Next'.
		}
		#endregion

		public abstract RichNotifyCollectionChangedEventArgs? ToEvent();

		public CollectionChangesQueue.Node Visit(ICollectionTrackingVisitor visitor)
		{
			var head = VisitCore(visitor);

			if (Next is not null)
			{
				// Search for the tail
				var node = head;
				while (node.Next is not null)
				{
					node = node.Next;
				}

				// And append the callbacks of the Next change
				node.Next = Next.Visit(visitor);
			}

			return head;
		}

		protected abstract CollectionChangesQueue.Node VisitCore(ICollectionTrackingVisitor visitor);
	}
}
