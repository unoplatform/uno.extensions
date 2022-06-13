using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Umbrella.Presentation.Feeds.Tests.Collections._TestUtils
{
	public class Count
	{
		public uint Value { get; }

		private Count(uint value) => Value = value;

		public static implicit operator Count(uint count) => new Count(count);
	}

	public interface IVectorChangeDescriptor
	{
		object? Sender { get; }

		string? SenderName { get; }

		CollectionChange? Change { get; }

		uint? Index { get; }

		uint? CollectionCount { get; }

		void ShouldMatch(object identifier, IVectorChangedEventArgs actual, uint count, object[] snapshot);
	}

	public class VectorChangeDescriptor<T> : IVectorChangeDescriptor
	{
		public static VectorChangeDescriptor<T> Empty { get; } = new();

		private VectorChangeDescriptor()
		{
		}

		public VectorChangeDescriptor(object sender, string name)
		{
			Sender = sender;
			SenderName = name;
		}

		private VectorChangeDescriptor(VectorChangeDescriptor<T> previous)
		{
			Sender = previous.Sender;
			SenderName = previous.SenderName;
			Change = previous.Change;
			Index = previous.Index;
			CollectionCount = previous.CollectionCount;
			CollectionAssertions = new List<Action<string, object[]>>(previous.CollectionAssertions);
			Collection = previous.Collection;
		}

		public object? Sender { get; private set; }

		public string? SenderName { get; private set; }

		public CollectionChange? Change { get; private set; }

		public uint? Index { get; private set; }

		public uint? CollectionCount { get; private set; }

		public List<Action<string, object[]>> CollectionAssertions { get; } = new List<Action<string, object[]>>();

		public T[]? Collection { get; private set; }


		public static implicit operator VectorChangeDescriptor<T>((CollectionChange change, uint index) values)
			=> new VectorChangeDescriptor<T>
			{
				Change = values.change,
				Index = values.index
			};

		public static VectorChangeDescriptor<T> operator &(VectorChangeDescriptor<T> current, (CollectionChange change, uint index) x)
			=> new VectorChangeDescriptor<T>(current)
			{
				Change = x.change,
				Index = x.index
			};

		public static VectorChangeDescriptor<T> operator &(VectorChangeDescriptor<T> current, (uint index, T expected) change)
			=> new VectorChangeDescriptor<T>(current)
			{
				CollectionAssertions =
				{
					(identifier, snapshot) =>
					{
						Assert.IsTrue(change.index < snapshot.Length, $"Change {identifier}: The expected item @{change.index} is not present in result content.");
						Assert.AreEqual(change.expected, snapshot[change.index], $"Change {identifier}: The item @{change.index} does not match the expectations.");
					}
				}
			};

		public static VectorChangeDescriptor<T> operator &(VectorChangeDescriptor<T> current, (uint index, Func<T, bool> assert) change)
			=> new VectorChangeDescriptor<T>(current)
			{
				CollectionAssertions =
				{
					(identifier, snapshot) =>
					{
						Assert.IsTrue(change.index < snapshot.Length, $"Change {identifier}: The expected item @{change.index} is not present in result content.");
						Assert.IsTrue(change.assert((T) snapshot[change.index]), $"Change {identifier}: The item @{change.index} does not match the expectations.");
					}
				}
			};

		public static VectorChangeDescriptor<T> operator &(VectorChangeDescriptor<T> current, Func<T[], bool> assert)
			=> new VectorChangeDescriptor<T>(current)
			{
				CollectionAssertions =
				{
					(identifier, snapshot) =>
					{
						Assert.IsTrue(assert(snapshot.Cast<T>().ToArray()), $"Change {identifier}: The snapshot does not match the expectation.");
					}
				}
			};

		public static VectorChangeDescriptor<T> operator &(VectorChangeDescriptor<T> current, T[] values)
			=> new VectorChangeDescriptor<T>(current)
			{
				Collection = values
			};

		public static VectorChangeDescriptor<T> operator &(VectorChangeDescriptor<T> current, Count count)
			=> new VectorChangeDescriptor<T>(current)
			{
				CollectionCount = count.Value
			};

		public void ShouldMatch(object identifier, IVectorChangedEventArgs actual, uint count, object[] snapshot)
		{
			if (this == Empty)
			{
				throw new InvalidOperationException("Tou must define at least one value to validate !");
			}

			// Defined assertions
			if (Change.HasValue)
			{
				Assert.AreEqual(Change.Value, actual.CollectionChange, $"Change {identifier}: Change type mismatch.");
			}
			if (Index.HasValue)
			{
				Assert.AreEqual(Index.Value, actual.Index, $"Change {identifier}: Index mismatch.");
			}
			if (CollectionCount.HasValue)
			{
				Assert.AreEqual(CollectionCount.Value, count, $"Change {identifier}: Count mismatch.");
			}
			if (Collection != null)
			{
				CollectionAssert.AreEqual(Collection, snapshot, $"Change {identifier}: Content mismatch.");
			}
			if (CollectionAssertions?.Count>0)
			{
				foreach (var assertion in CollectionAssertions)
				{
					assertion(identifier?.ToString() ?? string.Empty, snapshot);
				}
			}

			// Common assertions
			if (Change.HasValue && Index.HasValue)
			{
				switch (Change)
				{
					case CollectionChange.ItemInserted:
					case CollectionChange.ItemChanged:
						Assert.IsTrue(Index < snapshot.Length, $"Change {identifier}: Has an index ({Index}) greater or equals to the count ({snapshot.Length}).");
						break;

					case CollectionChange.ItemRemoved:
					case CollectionChange.Reset:
						Assert.IsTrue(Index <= snapshot.Length, $"Change {identifier}: Has an index ({Index}) greater to the count ({snapshot.Length}).");
						break;
				}
			}
			Assert.AreEqual(count, (uint)snapshot.Length, $"Change {identifier}: The enumeration did not produced the declared count of items.");
		}
	}
}
