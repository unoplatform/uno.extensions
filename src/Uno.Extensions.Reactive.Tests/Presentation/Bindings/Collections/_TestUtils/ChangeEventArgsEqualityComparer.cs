using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation.Collections;

namespace Umbrella.Presentation.Feeds.Tests.Collections._TestUtils
{
	internal class ChangeEventArgsEqualityComparer : IEqualityComparer<(object? sender, NotifyCollectionChangedEventArgs args)>, IEqualityComparer<(object? sender, IVectorChangedEventArgs args)>
	{
		public static ChangeEventArgsEqualityComparer Instance { get; } = new ChangeEventArgsEqualityComparer();

		private ChangeEventArgsEqualityComparer()
		{
		}

		public bool Equals((object? sender, IVectorChangedEventArgs args) left, (object? sender, IVectorChangedEventArgs args) right)
			=> left.sender == right.sender
			&& left.args.CollectionChange == right.args.CollectionChange
			&& left.args.Index == right.args.Index;

		public bool Equals((object? sender, NotifyCollectionChangedEventArgs args) left, (object? sender, NotifyCollectionChangedEventArgs args) right)
			=> left.sender == right.sender
			&& left.args.Action == right.args.Action
			&& left.args.OldStartingIndex == right.args.OldStartingIndex
			&& left.args.NewStartingIndex == right.args.NewStartingIndex
			&& (left.args.OldItems?.Cast<object>().SequenceEqual(right.args.OldItems!.Cast<object>(), EqualityComparer<object>.Default) ?? right.args.OldItems == null)
			&& (left.args.NewItems?.Cast<object>().SequenceEqual(right.args.NewItems!.Cast<object>(), EqualityComparer<object>.Default) ?? right.args.NewItems == null);

		public int GetHashCode((object? sender, IVectorChangedEventArgs args) obj) 
			=> obj.args.GetHashCode() 
			^ obj.args.CollectionChange.GetHashCode() 
			^ obj.args.Index.GetHashCode();

		public int GetHashCode((object? sender, NotifyCollectionChangedEventArgs args) obj)
			=> (obj.sender?.GetHashCode() ?? 0)
			^ obj.args.Action.GetHashCode()
			^ obj.args.OldStartingIndex.GetHashCode()
			^ obj.args.NewStartingIndex.GetHashCode();
	}
}
