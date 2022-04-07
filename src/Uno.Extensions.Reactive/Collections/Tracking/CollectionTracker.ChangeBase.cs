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

	private abstract class ChangeBase : IChange, ICollectionTrackingCallbacks, CollectionChangesQueue.INode
	{
		protected readonly List<object> _before;
		protected readonly List<object> _after;

		public ChangeBase(int at, int capacity)
		{
			Starts = at;
			Ends = at;

			_before = new List<object>(capacity);
			_after = new List<object>(capacity);
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

		#region ICollectionTrackingCallbacks
		void ICollectionTrackingCallbacks.Prepend(BeforeCallback callback) => _before.Add(callback);
		void ICollectionTrackingCallbacks.Prepend(ICompositeCallback child) => _before.Add(child);
		void ICollectionTrackingCallbacks.Append(AfterCallback callback) => _after.Add(callback);
		void ICollectionTrackingCallbacks.Append(ICompositeCallback child) => _after.Add(child);
		#endregion

		#region CollectionChangesQueue.INode
		CollectionChangesQueue.INode? CollectionChangesQueue.INode.Next => Next;

		public abstract RichNotifyCollectionChangedEventArgs ToEvent();

		protected virtual void RaiseTo(CollectionChangesQueue.IHandler handler, bool silently)
		{
			if (silently)
			{
				handler.ApplySilently(ToEvent());
			}
			else
			{
				handler.Raise(ToEvent());
			}
		}

		void CollectionChangesQueue.INode.ApplyTo(CollectionChangesQueue.IHandler handler, bool silently)
		{
			foreach (var before in _before)
			{
				switch (before)
				{
					case BeforeCallback callback:
						callback();
						break;

					case ICompositeCallback child:
						child.Invoke(CallbackPhase.Before | CallbackPhase.Main, silently);
						break;

					default:
						throw new InvalidOperationException("Unexpected before action");
				}
			}

			foreach (var after in _after.OfType<ICompositeCallback>())
			{
				after.Invoke(CallbackPhase.Before, silently);
			}

			RaiseTo(handler, silently);

			foreach (var before in _before.OfType<ICompositeCallback>())
			{
				before.Invoke(CallbackPhase.After, silently);
			}

			foreach (var after in _after.OfType<ICompositeCallback>())
			{
				after.Invoke(CallbackPhase.Main, silently);
			}

			foreach (var after in _after)
			{
				switch (after)
				{
					case AfterCallback callback:
						callback();
						break;

					case ICompositeCallback child:
						//child.Invoke();
						child.Invoke(CallbackPhase.After, silently);
						break;

					default:
						throw new InvalidOperationException("Unexpected after action");
				}
			}
		}

		void CollectionChangesQueue.INode.RunBeforeCallbacks()
		{
			foreach (var before in _before)
			{
				switch (before)
				{
					case BeforeCallback callback:
						callback();
						break;

					case ICompositeCallback child:
						//child.InvokeCallbacksOnly();
						child.Invoke(CallbackPhase.Before | CallbackPhase.Main, silently: true);
						break;

					default:
						throw new InvalidOperationException("Unexpected before action");
				}
			}

			foreach (var after in _after.OfType<ICompositeCallback>())
			{
				after.Invoke(CallbackPhase.Before, silently: true);
			}
		}

		void CollectionChangesQueue.INode.RunAfterCallbacks()
		{
			foreach (var before in _before.OfType<ICompositeCallback>())
			{
				before.Invoke(CallbackPhase.After, silently: true);
			}

			foreach (var after in _after)
			{
				switch (after)
				{
					case AfterCallback callback:
						callback();
						break;

					case ICompositeCallback child:
						//child.InvokeCallbacksOnly();
						child.Invoke(CallbackPhase.Main | CallbackPhase.After, silently: true);
						break;

					default:
						throw new InvalidOperationException("Unexpected before action");
				}
			}
		}
		#endregion
	}
}
