using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.Tests._Utils;

namespace Uno.Extensions.Reactive.Tests.Collections.Tracking;

internal abstract class CollectionTrackerTester<TCollection, T>
	where TCollection : IEnumerable
{
	private readonly TCollection _previous;
	private TCollection? _updated;
	private IEqualityComparer<T>? _itemComparer;
	private IEqualityComparer<T>? _itemVersionComparer;

	public CollectionTrackerTester(TCollection previous, TCollection? updated)
	{
		_previous = previous;
		_updated = updated;
	}

	public CollectionTrackerTester<TCollection, T> To(params T[] updated)
	{
		_updated = Create(updated);

		return this;
	}

	protected abstract TCollection Create(T[] items);

	public CollectionTrackerTester<TCollection, T> With(IEqualityComparer<T>? itemComparer = null, IEqualityComparer<T>? itemVersionComparer = null)
	{
		_itemComparer = itemComparer;
		_itemVersionComparer = itemVersionComparer;

		return this;
	}

	public void ShouldBeEmpty()
	{
		ShouldBe();
	}

	protected abstract CollectionUpdater GetUpdater(ItemComparer<T> comparer, TCollection previous, TCollection updated, ICollectionUpdaterVisitor visitor);
	protected abstract CollectionChangeSet GetChanges(ItemComparer<T> comparer, TCollection previous, TCollection updated);
	protected abstract IEnumerable<T> AsEnumerable(TCollection collection);

	public void ShouldBe(params NotifyCollectionChangedEventArgs[] expected)
	{
		if (_updated is null)
		{
			Assert.Fail("To collection has not been set.");
		}

		AssertUsingUpdater(expected);
		AssertUsingCollectionChangeEventArgs(expected);
		AssertUsingCollectionChangeVisitor(expected);
	}

	public void AssertUsingUpdater(NotifyCollectionChangedEventArgs[] expected)
	{
		var visitor = new TestVisitor();
		var updater = GetUpdater(new ItemComparer<T>(_itemComparer, _itemVersionComparer), _previous, _updated!, visitor);
		var previousEnumerable = AsEnumerable(_previous);
		var updatedEnumerable = AsEnumerable(_updated!);

		IEnumerable<NotifyCollectionChangedEventArgs> GetCollectionChanges()
		{
			// Note: we use reflexion here since it's only for debug output

			var node = updater
				.GetType()
				.GetField("_head", global::System.Reflection.BindingFlags.Instance | global::System.Reflection.BindingFlags.NonPublic)
				?.GetValue(updater) as CollectionUpdater.Update;

			while(node != null)
			{
				var args = default(RichNotifyCollectionChangedEventArgs);
				try
				{
					args = node.Event;
				}
				catch (Exception) { }

				if (args != null)
				{
					yield return args;
				}
				node = node.Next;
			}
		}

		Console.WriteLine($"Expected: \r\n{expected.ToOutputString()}");
		Console.WriteLine();
		Console.WriteLine($"Actual: \r\n{GetCollectionChanges().ToOutputString()}");

		var handler = new Handler(expected);
		updater.DequeueChanges(handler);

		Assert.AreEqual(expected.Length, handler.EventsCount);

		var previousDuplicates = previousEnumerable.Count() - previousEnumerable.Distinct(_itemComparer ?? EqualityComparer<T>.Default).Count();
		var updatedDuplicates = updatedEnumerable.Count() - updatedEnumerable.Distinct(_itemComparer ?? EqualityComparer<T>.Default).Count();

		var added = updatedEnumerable.Except(previousEnumerable, _itemComparer ?? EqualityComparer<T>.Default).ToArray();
		var removed = previousEnumerable.Except(updatedEnumerable, _itemComparer ?? EqualityComparer<T>.Default).ToArray();

		var kept1 = previousEnumerable.Except(removed).ToArray(); // either updated or moved (or nothing at all)
		var kept2 = updatedEnumerable.Except(added).ToArray();

		Assert.HasCount(kept1.Length, kept2);

		var notUpdated = kept1.Except(kept2, _itemComparer ?? EqualityComparer<T>.Default).ToArray(); // moved (or nothing at all): items for which the vistiro should not have been invoked
		var updated = kept1.Except(notUpdated).Join(kept2, l => l, r => r, (originalItem, updatedItem) => (originalItem, updatedItem), _itemComparer ?? EqualityComparer<T>.Default).ToArray();

		Console.WriteLine($@"
Detected changes using Linq: 
Added ({added.Length}): 
	{string.Join("\r\n\t", added)}
Removed ({removed.Length}): 
	{string.Join("\r\n\t", removed)}
Updated ({updated.Length}): 
	{string.Join("\r\n\t", updated.Select(items => $"{items.Item1} => {items.Item2}"))}
Moved or untouched ({notUpdated.Length}):
	{string.Join("\r\n\t", notUpdated)}");

		if (updated.Length + added.Length + removed.Length != visitor.Pending
			&& updated.Length + added.Length + removed.Length + Math.Abs(updatedDuplicates - previousDuplicates) != visitor.Pending
			&& updated.Length + added.Length + removed.Length + previousDuplicates != visitor.Pending
			&& updated.Length + added.Length + removed.Length + updatedDuplicates != visitor.Pending)
		{
			Assert.Fail($"Did not invoke the visitor for each item! (actual: {visitor.Pending})");
		}

		visitor.AssertAllRaised();
	}

	public void AssertUsingCollectionChangeEventArgs(NotifyCollectionChangedEventArgs[] expected)
	{
		var changeSet = GetChanges(new ItemComparer<T>(_itemComparer, _itemVersionComparer), _previous, _updated!);
		var actual = changeSet.ToCollectionChanges().ToArray();
		var comparer = new NotifyCollectionChangedComparer(MyClassComparer.Instance);

		actual.Length.Should().Be(expected.Length);

		for (var i = 0; i < actual.Length; i++)
		{
			comparer.Equals(expected[i], actual[i]).Should().BeTrue();
		}
	}

	public void AssertUsingCollectionChangeVisitor(NotifyCollectionChangedEventArgs[] expected)
	{
		var changeSet = GetChanges(new ItemComparer<T>(_itemComparer, _itemVersionComparer), _previous, _updated!);

		IList actual;
		switch(changeSet)
		{
			case CollectionChangeSet<MyClass> classChangeSet:
			{
				var visitor = new TestCollectionChangeSet<MyClass>();
				classChangeSet.Visit(visitor);
				actual = visitor.Result;
				break;
			}
			case CollectionChangeSet<int> valueTypeChangeSet:
			{
				var visitor = new TestCollectionChangeSet<int>();
				valueTypeChangeSet.Visit(visitor);
				actual = visitor.Result;
				break;
			}
			case CollectionChangeSet<object?> objectChangeSet:
			{
				var visitor = new TestCollectionChangeSet<object?>();
				objectChangeSet.Visit(visitor);
				actual = visitor.Result;
				break;
			}
			default: throw new ArgumentException($"Type {typeof(T).Name} not supported");
		}

		var comparer = new NotifyCollectionChangedComparer(MyClassComparer.Instance);
		for (var i = 0; i < actual.Count; i++)
		{
			comparer.Equals(expected[i], actual[i]).Should().BeTrue();
		}
	}

	private class Handler : CollectionUpdater.IHandler
	{
		private readonly NotifyCollectionChangedEventArgs[] _expected;
		private int _expectedIndex = 0;

		public int EventsCount { get; private set; }

		public int Added { get; private set; }

		public int Removed { get; private set; }

		public int Replaced { get; private set; }

		public Handler(NotifyCollectionChangedEventArgs[] expected) => _expected = expected;

		public void Raise(RichNotifyCollectionChangedEventArgs arg)
		{
			EventsCount++;
			Assert.IsTrue(new NotifyCollectionChangedComparer(MyClassComparer.Instance).Equals(_expected[_expectedIndex++], arg));

			switch (arg.Action)
			{
				case NotifyCollectionChangedAction.Add:
					Added += arg.NewItems!.Count;
					break;

				case NotifyCollectionChangedAction.Remove:
					Removed += arg.OldItems!.Count;
					break;

				case NotifyCollectionChangedAction.Replace:
					Replaced += arg.NewItems!.Count;
					break;

				case NotifyCollectionChangedAction.Reset:
					throw new InvalidOperationException("Tracker should not generate Reset");
			}
		}

		public void ApplySilently(RichNotifyCollectionChangedEventArgs arg)
			=> throw new InvalidOperationException("Tracker should not generate silent event since we never handle them in visitor.");
	}
}

internal class TestVisitor : ICollectionUpdaterVisitor
{
	private int _pending, _added, _equals, _replaced, _remove, _reset;

	public int Pending => _pending;
	public int Added => _added;
	public int Same => _equals;
	public int Replaced => _replaced;
	public int Removed => _remove;
	public int Reseted => _reset;

	public void AssertAllRaised()
	{
		Assert.AreEqual(Pending, Added + Same + Replaced + Removed + Reseted);
	}

	public void AddItem(object? item, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Prepend(Will);
		callbacks.Prepend(Did);

		void Will()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}
		}

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 2, 1) != 1)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _added);
		}
	}

	public void SameItem(object? original, object? updated, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Append(Did);

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _equals);
		};
	}

	public bool ReplaceItem(object? original, object? updated, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Prepend(Will);
		callbacks.Prepend(Did);

		return false;

		void Will()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}
		}

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 2, 1) != 1)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _replaced);
		}
	}

	public void RemoveItem(object? item, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Prepend(Will);
		callbacks.Prepend(Did);

		void Will()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}
		}

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 2, 1) != 1)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _remove);
		}
	}

	public void Reset(IList oldItems, IList newItems, ICollectionUpdateCallbacks callbacks)
	{
		Interlocked.Increment(ref _pending);
		var state = 0;

		callbacks.Prepend(Will);
		callbacks.Prepend(Did);

		void Will()
		{
			if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
			{
				throw new InvalidOperationException("Invalid state");
			}
		}

		void Did()
		{
			if (Interlocked.CompareExchange(ref state, 2, 1) != 1)
			{
				throw new InvalidOperationException("Invalid state");
			}

			Interlocked.Increment(ref _reset);
		}
	}
}

internal class TestCollectionChangeSet<T> : CollectionChangeSetVisitorBase<T>
{
	public List<RichNotifyCollectionChangedEventArgs> Result { get; }

	public TestCollectionChangeSet()
	{
		Result = new List<RichNotifyCollectionChangedEventArgs>();
	}

	/// <inheritdoc />
	public override void Add(IReadOnlyList<T> items, int index)
		=> Result.Add(RichNotifyCollectionChangedEventArgs.AddSome((IList)items.ToList(), index));

	/// <inheritdoc />
	public override void Move(IReadOnlyList<T> items, int fromIndex, int toIndex)
		=> Result.Add(RichNotifyCollectionChangedEventArgs.MoveSome((IList)items.ToList(), fromIndex, toIndex));

	/// <inheritdoc />
	public override void Replace(IReadOnlyList<T> original, IReadOnlyList<T> updated, int index)
		=> Result.Add(RichNotifyCollectionChangedEventArgs.ReplaceSome((IList)original.ToList(), (IList)updated.ToList(), index));

	/// <inheritdoc />
	public override void Remove(IReadOnlyList<T> items, int index)
		=> Result.Add(RichNotifyCollectionChangedEventArgs.RemoveSome((IList)items.ToList(), index));

	/// <inheritdoc />
	public override void Reset(IReadOnlyList<T> oldItems, IReadOnlyList<T> newItems)
		=> Result.Add(RichNotifyCollectionChangedEventArgs.Reset((IList)oldItems.ToList(), (IList)newItems.ToList()));
}


internal class MyClass
{
	public int Version { get; }

	public int Value { get; }

	private MyClass(int value, int version)
	{
		Value = value;
		Version = version;
	}

	public override int GetHashCode() => Value;

	public override bool Equals(object? obj) => obj is MyClass c && Value == c.Value && Version == c.Version;

	public static implicit operator MyClass(int value) => new MyClass(value, 0);

	public static implicit operator MyClass((int value, int version) values) => new MyClass(values.value, values.version);

	public static explicit operator int(MyClass obj) => obj.Value;

	public override string ToString() => Version > 0 ? $"{Value}v{Version}" : Value.ToString();
}

internal static class Given_CollectionAnalyzer_Extensions
{
	public static void ShouldBe(
		this ICollection<NotifyCollectionChangedEventArgs> changes,
		params NotifyCollectionChangedEventArgs[] expected)
	{
		Console.WriteLine($"Expected: \r\n{expected.ToOutputString()}");
		Console.WriteLine();
		Console.WriteLine($"Actual: \r\n{changes.ToOutputString()}");

		CollectionAssert.AreEqual(expected, changes.ToArray(), new NotifyCollectionChangedComparer(MyClassComparer.Instance));
	}

	public static string ToOutputString(this IEnumerable<NotifyCollectionChangedEventArgs> args)
		=> "\t" + string.Join("\r\n\t", args.Select((arg, i) => $"{i:00} - {ToOutputString(arg)}"));

	public static string ToOutputString(this NotifyCollectionChangedEventArgs arg) =>
		$"{arg.Action}: " +
		$"@ {arg.OldStartingIndex} => [{string.Join(", ", arg.OldItems?.Cast<object>().Select(MyClassComparer.GetData).Select(d => d.version > 0 ? $"{d.value}v{d.version}" : $"{d.value}") ?? new string[0])}] / " +
		$"@ {arg.NewStartingIndex} => [{string.Join(", ", arg.NewItems?.Cast<object>().Select(MyClassComparer.GetData).Select(d => d.version > 0 ? $"{d.value}v{d.version}" : $"{d.value}") ?? new string[0])}]";
}

internal class MyClassComparer : IEqualityComparer<object?>
{
	public static MyClassComparer Instance { get; } = new MyClassComparer();

	public new bool Equals(object? left, object? right) => GetData(left).Equals(GetData(right));

	public int GetHashCode(object? obj)
	{
		var (value, version) = GetData(obj);
		return value + version;
	}

	public static (int value, int version) GetData(object? item)
	{
		if (item is MyClass c)
		{
			return (c.Value, c.Version);
		}
		else if (item is int i)
		{
			return (i, 0);
		}
		else if (item is ValueTuple<int, int> v)
		{
			return v;
		}

		throw new InvalidOperationException();
	}
}
