using System;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Umbrella.Presentation.Feeds.Tests.Collections._TestUtils;

namespace Umbrella.Presentation.Feeds.Tests.Collections._TestUtils
{
	internal class MyCollectionViewGroup : ICollectionViewGroup
	{
		private readonly object? _group;
		private readonly Func<IObservableVector<object?>> _items;

		public MyCollectionViewGroup(object? group)
		{
			_group = group;
			_items = () => throw new NotSupportedException("Usable only for equality");
		}

		public MyCollectionViewGroup(object? group, IObservableVector<object?> items) 
		{
			_group = group;
			_items = () => items;
		}


		public object? Group => _group;
		public IObservableVector<object?> GroupItems => _items();

		public override int GetHashCode()
		{
			return Group?.GetHashCode() ?? 0;
		}

		public override bool Equals(object? obj)
		{
			return obj is ICollectionViewGroup other && (Group?.Equals(other.Group) ?? other.Group is null);
		}
	}
}
