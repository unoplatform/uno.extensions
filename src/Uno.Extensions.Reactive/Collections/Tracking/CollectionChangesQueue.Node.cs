using System;
using System.Collections.Generic;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

partial class CollectionChangesQueue
{
	internal class Node : ICollectionTrackingCallbacks
	{
		private readonly List<object> _before = new();
		private readonly List<object> _after = new();

		public RichNotifyCollectionChangedEventArgs? Event { get; set; }

		public Node? Next { get; set; }

		public Node(RichNotifyCollectionChangedEventArgs? @event)
		{
			Event = @event;
		}

		public Node()
		{
		}

		void ICollectionTrackingCallbacks.Prepend(BeforeCallback callback) => _before.Add(callback);

		void ICollectionTrackingCallbacks.Prepend(ICompositeCallback child) => _before.Add(child);

		void ICollectionTrackingCallbacks.Append(AfterCallback callback) => _after.Add(callback);

		void ICollectionTrackingCallbacks.Append(ICompositeCallback child) => _after.Add(child);

		public void FlushTo(Node other)
		{
			other._before.AddRange(_before);
			other._after.AddRange(_after);

			_before.Clear();
			_after.Clear();
		}

		public void ApplyTo(IHandler handler, bool silently)
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

			if (Event is not null)
			{
				if (silently)
				{
					handler.ApplySilently(Event);
				}
				else
				{
					handler.Raise(Event);
				}
			}

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

			Next?.ApplyTo(handler, silently);
		}

		public void RunBeforeCallbacks()
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

			Next?.RunBeforeCallbacks();
		}

		public void RunAfterCallbacks()
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

			Next?.RunAfterCallbacks();
		}

		/// <inheritdoc />
		public override string ToString()
			=> $"{Event?.ToString() ?? "[callbacks_only]"} (b: {_before.Count} | a: {_after.Count})";
	}
}
