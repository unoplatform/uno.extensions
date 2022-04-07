using System;
using System.Linq;

namespace nVentive.Umbrella.Collections.Tracking;

public sealed partial class CollectionChangesQueue
{
	internal interface INode
	{
		/// <summary>
		/// The following node, if any
		/// </summary>
		INode? Next { get; }

		/// <summary>
		/// Applies this node to an handler
		/// </summary>
		void ApplyTo(IHandler handler, bool silently);

		/// <summary>
		/// Executes all the callbacks that should normally be invoked before the event of this change is neeing raised.
		/// <remarks>This method is dedicated to convertion of a changes queue to 'Reset'</remarks>
		/// </summary>
		void RunBeforeCallbacks();

		/// <summary>
		/// Executes all the callbacks that should normally be invoked after the event of this change has been raised.
		/// <remarks>This method is dedicated to convertion of a changes queue to 'Reset'</remarks>
		/// </summary>
		void RunAfterCallbacks();

		/// <summary>
		/// LEGACY DO NOT USE - Gives a way to convert the queue to a basic collection of collection changed event args
		/// </summary>
		RichNotifyCollectionChangedEventArgs ToEvent();
	}

	private class ArgsToNodeAdapter : INode
	{
		private readonly RichNotifyCollectionChangedEventArgs _args;

		public ArgsToNodeAdapter(RichNotifyCollectionChangedEventArgs args)
		{
			_args = args;
		}

		public INode? Next { get; } = null;

		public void ApplyTo(IHandler handler, bool silently)
		{
			if (silently)
			{
				handler.Raise(_args);
			}
			else
			{
				handler.ApplySilently(_args);
			}
		}

		public RichNotifyCollectionChangedEventArgs ToEvent() => _args;

		public void RunBeforeCallbacks() { }

		public void RunAfterCallbacks() { }

		/// <inheritdoc />
		public override string ToString()
			=> $"RESET: {_args.Action} {_args.OldItems?.Count}/{_args.NewItems?.Count} @ {_args.OldStartingIndex}/{_args.NewStartingIndex}";
	}
}
