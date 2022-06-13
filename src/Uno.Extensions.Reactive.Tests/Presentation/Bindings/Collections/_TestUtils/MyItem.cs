using System;
using System.Linq;
using System.Windows.Markup;
using Uno.Equality;

namespace Umbrella.Feeds.Tests._TestUtils
{
	public class MyItem : IKeyEquatable, IKeyEquatable<MyItem>, IEquatable<MyItem>
	{
		public int Id { get; }

		public int Version { get; }

		public MyItem(int id) => Id = id;

		public MyItem(int id, int version)
		{
			Id = id;
			Version = version;
		}

		public MyItem Update() => new MyItem(Id, Version + 1);

		public MyItem Update(int version)
		{
			if (version <= Version)
			{
				throw new InvalidOperationException("New version must be greater than the curren one for an update operation");
			}

			return new MyItem(Id, version);
		}

		int IKeyEquatable.GetKeyHashCode() => Id;
		int IKeyEquatable<MyItem>.GetKeyHashCode() => Id;
		bool IKeyEquatable.KeyEquals(object obj) 
			=> obj is MyItem other && Id == other.Id
			|| obj is int id && Id == id
			|| obj is ValueTuple<int, int> values && Id == values.Item1;
		bool IKeyEquatable<MyItem>.KeyEquals(MyItem other) => Id == other?.Id;

		public override int GetHashCode() => Id;
		public override bool Equals(object obj)
			=> (obj is MyItem other && Equals(other))
			|| (obj is int id && Equals((MyItem) id))
			|| (obj is ValueTuple<int, int> values && Equals((MyItem) values));
		public bool Equals(MyItem other) => other != null && Id == other.Id && Version == other.Version;

		public override string ToString() => Version > 0 ? $"{Id}v{Version}" : Id.ToString();

		public static implicit operator MyItem(int id) => new MyItem(id);

		public static implicit operator MyItem((int id, int version) values) => new MyItem(values.id, values.version);

		public static bool operator ==(MyItem item, int id) => item?.Equals(id) ?? false;
		public static bool operator !=(MyItem item, int id) => item?.Equals(id) ?? true;

		public static bool operator ==(MyItem item, (int id, int version) values) => item?.Equals(values) ?? false;
		public static bool operator !=(MyItem item, (int id, int version) values) => item?.Equals(values) ?? true;
	}
}
