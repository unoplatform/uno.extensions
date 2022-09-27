using System;
using System.Linq;
using Uno.Extensions.Reactive.Utils;
using static System.Collections.Specialized.NotifyCollectionChangedAction;

namespace Uno.Extensions.Collections.Tracking;

partial class CollectionAnalyzer
{
	private class _Event<T> : Change<T>
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
		protected internal override void Visit(ICollectionChangeSetVisitor<T> visitor)
		{
			switch (_args.Action)
			{
				case Add:
					visitor.Add(_args.NewItems!.AsTypedReadOnlyList<T>(), _args.NewStartingIndex);
					return;

				case Remove:
					visitor.Remove(_args.OldItems!.AsTypedReadOnlyList<T>(), _args.OldStartingIndex);
					return;

				case Move:
					visitor.Move(_args.OldItems!.AsTypedReadOnlyList<T>(), _args.OldStartingIndex, _args.NewStartingIndex);
					break;

				case Replace:
					visitor.Replace(_args.OldItems!.AsTypedReadOnlyList<T>(), _args.NewItems!.AsTypedReadOnlyList<T>(), _args.OldStartingIndex);
					return;

				case Reset:
					visitor.Reset(_args.ResetOldItems!.AsTypedReadOnlyList<T>(), _args.ResetNewItems!.AsTypedReadOnlyList<T>());
					return;

				default:
					throw new ArgumentOutOfRangeException(nameof(_args), _args.Action, $"Action '{_args.Action}' not supported.");
			}
		}

		/// <inheritdoc />
		protected override CollectionUpdater.Update ToUpdaterCore(ICollectionUpdaterVisitor visitor)
		{
			var arg = _args;
			switch (arg.Action)
			{
				case Add:
					return _Add<T>.Visit(arg, visitor);

				case Remove:
					return _Remove<T>.Visit(arg, visitor);

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
