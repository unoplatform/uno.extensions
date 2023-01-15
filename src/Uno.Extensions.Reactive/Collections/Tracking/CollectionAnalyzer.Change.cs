using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Collections.Tracking;

partial class CollectionAnalyzer
{
	internal abstract class Change<T> : ICollectionChange
	{
		public Change(int at)
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
		public Change<T>? Next { get; set; }

		public abstract RichNotifyCollectionChangedEventArgs? ToEvent();

		/// <summary>
		/// Converts this change linked list to a <see cref="CollectionUpdater.Update"/> linked list.
		/// </summary>
		/// <param name="visitor"></param>
		/// <returns></returns>
		public CollectionUpdater.Update ToUpdater(ICollectionUpdaterVisitor visitor)
		{
			var head = ToUpdaterCore(visitor);

			if (Next is not null)
			{
				// Search for the tail
				var node = head;
				while (node.Next is not null)
				{
					node = node.Next;
				}

				// And append the callbacks of the Next change
				node.Next = Next.ToUpdater(visitor);
			}

			return head;
		}

		protected abstract CollectionUpdater.Update ToUpdaterCore(ICollectionUpdaterVisitor visitor);

		protected internal abstract void Visit(ICollectionChangeSetVisitor<T> visitor);
	}

	/// <summary>
	/// A base change specialized from changes that are only altering entities within a collection
	/// (i.e. they are not impacting indices - e.g. Replace and Same)
	/// </summary>
	private abstract class EntityChange<T> : Change<T>
	{
		protected readonly int _indexOffset;
		protected readonly List<T> _oldItems = new();
		protected readonly List<T> _newItems = new();

		public new EntityChange<T>? Next
		{
			get => base.Next as EntityChange<T>;
			set => base.Next = value;
		}

		public EntityChange(int at, int indexOffset)
			: base(at)
		{
			_indexOffset = indexOffset;
			Ends = at;
		}

		public void Append(T oldItem, T newItem)
		{
			_oldItems.Add(oldItem);
			_newItems.Add(newItem);
			Ends++;

			// Try to merge with the next if possible
			// Note: The next must be a _Replace when in 'edition' mode.
			var next = Next; // Use local variable to avoid multiple cast
			while (next != null && Ends == next.Starts)
			{
				_oldItems.AddRange(next._oldItems);
				_newItems.AddRange(next._newItems);
				Ends = next.Ends;
				Next = next.Next;

				next = Next; //update local variable
			}
		}
	}
}
