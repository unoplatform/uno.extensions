using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking
{
	partial class CollectionTracker
	{
		private class ChangesBuffer
		{
			/*
			 * We generate 4 kinds of changes:
			 * - Add: are based on the RESULT index and impacts the RESULT index
			 * - Move: are based on the OLD index and impacts the RESULT index
			 * - Remove: are based on the OLD index and impacts the OLD index
			 * - Replace: are based on the OLD index
			 * 	
			 * 	As we always notify the Replace BEFORE other notifications and it does not impacts the RESULT index, 
			 * 	we can buffer them in parallel with add or move (works on OLD index - That's why we have dedicated buffers for the Replace operations).
			 * 	But if something impact the OLD index (i.e. Remove) we have to execute them before in order to re-sync indexes.
			 * 	
			 */

			private readonly int _oldItemsCount, _newItemsCount;
			private readonly int _eventArgsOffset;
			private readonly ICollectionTrackingVisitor _visitor;

			private readonly Head _head;
			private IChange _tail;
			private readonly _Same _same; // Note: We use a single '_Same' node for all updates
			private _Replace? _replaceHead;
			private readonly _Replace.CallbacksBuffer _replaceBuffer = new _Replace.CallbacksBuffer();

			public ChangesBuffer(int oldItemsCount, int newItemsCount, int eventArgsOffset, ICollectionTrackingVisitor visitor)
			{
				_oldItemsCount = oldItemsCount;
				_newItemsCount = newItemsCount;
				_eventArgsOffset = eventArgsOffset;
				_visitor = visitor;

				_tail = _head = new Head();
				_same = new _Same(Math.Min(_oldItemsCount + _newItemsCount, 32));
				//_replace = null; We keep it 'null'
			}

			private IChange Tail
			{
				get => _tail;
				set => _tail = _tail.Next = value;
			}

			public void Append(IChange change) => Tail = change;

			/// <summary>
			/// Flush the buffers and retrieve the full list of changes
			/// </summary>
			/// <returns></returns>
			public CollectionChangesQueue GetChanges()
			{
				// Start with the '_replace'
				IChange? head = _replaceHead, tail = _replaceHead;
				while (tail?.Next is not null)
				{
					tail = tail.Next;
				}

				// If we have some '_same', append it to the tail
				if (_same.HasCallbacks)
				{
					if (head is null)
					{
						head = _same;
						tail = _same;
					}
					else
					{
						tail = tail!.Next = _same;
					}
				}

				// Then append all other changes (Add / Move / Remove)
				// Note: as the '_head' is initialized with a 'Head', we can bypass it
				if (head is null)
				{
					head = _head.Next;
				}
				else
				{
					tail!.Next = _head.Next;
				}

				return new CollectionChangesQueue(head as ChangeBase);
			}

			public void Update(object oldItem, object newItem, int index)
			{
				// As they don't impact the 'result' index, replace-instance are buffered separately and will be inserted at the top of the changes collection.
				// Note: We use a single '_Same' node for all updates

				_visitor.SameItem(oldItem, newItem, _same);
			}

			public void Replace(object oldItem, object newItem, int index)
			{
				// As they don't impact the 'result' index, replaces are buffered separetly and will be inserted at the top of the changes collection.

				var isSilent = _visitor.ReplaceItem(oldItem, newItem, _replaceBuffer);
				if (_replaceHead == null)
				{
					_replaceHead = new _Replace(oldItem, newItem, index, _eventArgsOffset, isSilent, _replaceBuffer);

					return;
				}
				
				// Search the target node
				var node = _replaceHead;
				while (node.Next != null && node.Next.Starts < index)
				{
					node = node.Next;
				}

				// Then append the item to the selected nodes
				if (isSilent == node.IsSilent && node.Ends == index)
				{
					node.Append(oldItem, newItem, _replaceBuffer);
				}
				else if (node.Next == null || node.Next.Starts > index)
				{
					// Item is betwwen selected node and the next one, insert a new node.
					node.Next = new _Replace(oldItem, newItem, index, _eventArgsOffset, isSilent, _replaceBuffer)
					{
						Next = node.Next
					};
				}
			}

			public void Add(object item, int at, int max)
			{
				if (!(Tail is _Add add) || add.Ends != at)
				{
					Tail = add = new _Add(at, _eventArgsOffset, _visitor, max - at);
				}
				add.Append(item);
			}

			public void Move(object item, int from, int fromOffset, int to, int max)
			{
				if (!(Tail is _Move move) || move.Ends != from)
				{
					Tail = move = new _Move(from + fromOffset, to, _eventArgsOffset, max - to);
				}
				move.Append(item);
			}

			public void Remove(object item, int at)
			{
				if (!(Tail is _Remove remove) || remove.Ends != at)
				{
					Tail = remove = new _Remove(at, _eventArgsOffset, _visitor, Math.Max(_oldItemsCount - at, 4));
				}
				remove.Append(item);
			}
		}
	}
}
