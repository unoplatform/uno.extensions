using System;
using System.Linq;
using Umbrella.Feeds.Tests._TestUtils;
using Uno.Equality;

namespace Umbrella.Presentation.Feeds.Tests.Collections._TestUtils
{
	public class MyKey : MyItem
	{
		public MyKey(int id) : base(id) { }
		public MyKey(int id, int version) : base(id, version) { }

		int IKeyEquatable<MyKey>.GetKeyHashCode() => Id;
		bool IKeyEquatable<MyKey>.KeyEquals(MyKey other) => Id == other?.Id;
	}
}
