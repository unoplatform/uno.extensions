using System;
using System.Linq;

using static System.Collections.Specialized.NotifyCollectionChangedAction;

namespace Uno.Extensions.Collections.Tracking;

partial class CollectionAnalyzer
{
	internal class _Event : Change
	{
		private readonly RichNotifyCollectionChangedEventArgs _args;

		public _Event(RichNotifyCollectionChangedEventArgs args)
			: base(at: -1)
		{
			if (args.Action is not Add and not Remove and not Move and not Reset)
			{
				throw new ArgumentOutOfRangeException(nameof(args.Action), "Only Add, Remove, Move and Reset are supported.");
			}

			_args = args;
		}

		public override RichNotifyCollectionChangedEventArgs ToEvent()
			=> _args;

		/// <inheritdoc />
		protected override CollectionUpdater.Update ToUpdaterCore(ICollectionUpdaterVisitor visitor)
		{
			var arg = _args;
			switch (arg.Action)
			{
				case Add:
					return _Add.Visit(arg, visitor);

				case Remove:
					return _Remove.Visit(arg, visitor);

				case Move:
					return new CollectionUpdater.Update(_args);

				case Reset:
					var node = new CollectionUpdater.Update(_args);
					visitor.Reset(_args.ResetOldItems!, _args.ResetNewItems!, node);
					return node;

				default:
					throw new ArgumentOutOfRangeException(nameof(arg), arg.Action, $"Action '{arg.Action}' not supported.");
			}
		}

		/// <inheritdoc />
		public override string ToString()
			=> _args.ToString();
	}
}
