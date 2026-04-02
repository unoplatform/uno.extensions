using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_StateImpl : FeedTests
{
	[TestMethod]
	public async Task When_Create_Then_TaskDoNotLeak()
	{
		var sut = new StateImpl<string>(Context, Option<string>.None());

		var sub = sut.GetType().GetField("_subscription", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sut)!;
		var src = sub.GetType().GetField("_messages", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sub)!;
		var node = src.GetType().GetField("_current", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(src)!;
		var next = node.GetType().GetField("_next", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(node)!;
		var task = (Task)next.GetType().GetProperty("Task")!.GetValue(next)!;

		task.CreationOptions
			.HasFlag(TaskCreationOptions.AttachedToParent)
			.Should()
			.BeFalse("Creating the task attached to parent will prevent the current async context to complete as it will wait for the Next task to complete before completing itself.");
	}

	[TestMethod]
	public async Task When_Empty_Then_CanBeUpdatedByMessage()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.None()).Record();

		await sut.UpdateMessageAsync(msg => msg.Data("42"), CT);

		result.Should().Be(r => r
			.Message(Data.None, Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Empty_Then_CanBeUpdatedByValue()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.None()).Record();

		await sut.UpdateDataAsync(_ => "42", CT);

		result.Should().Be(r => r
			.Message(Data.None, Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Empty_Then_CanBeUpdated()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.None()).Record();

		await sut.UpdateAsync(_ => "42", CT);

		result.Should().Be(r => r
			.Message(Data.None, Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Value_Then_CanBeUpdatedByMessage()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.Some("0")).Record();

		await sut.UpdateMessageAsync(msg => msg.Data("42"), CT);

		result.Should().Be(r => r
			.Message("0", Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Value_Then_CanBeUpdatedByValue()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.Some("0")).Record();

		await sut.UpdateDataAsync(_ => "42", CT);

		result.Should().Be(r => r
			.Message("0", Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	[TestMethod]
	public async Task When_Value_Then_CanBeUpdated()
	{
		var (result, sut) = new StateImpl<string>(Context, Option<string>.Some("0")).Record();

		await sut.UpdateAsync(_ => "42", CT);

		result.Should().Be(r => r
			.Message("0", Progress.Final, Error.No)
			.Message("42", Progress.Final, Error.No, Changed.Data));
	}

	#region Compaction
	[TestMethod]
	public async Task When_ManyUpdates_Then_ActiveUpdatesCompacted()
	{
		const int count = 100;
		var (result, sut) = new StateImpl<int>(Context, Option.Some(0)).Record();

		for (var i = 1; i <= count; i++)
		{
			await sut.UpdateAsync(_ => i, CT);
		}

		await result.WaitForMessages(count + 1, CT);
		result.Last().Current.Data.SomeOrDefault().Should().Be(count);

		// ValueFeed completes immediately, so _isParentCompleted becomes true
		// and TryCompact prunes all applied updates after each IncrementalUpdate.
		// Allow a short delay for the ContinueWith to propagate.
		var activeCount = GetActiveUpdatesCount(sut);
		activeCount.Should().BeLessOrEqualTo(
			1,
			$"_activeUpdates should be compacted after ValueFeed completes, " +
			$"but {activeCount} Update records remain");
	}

	[TestMethod]
	public async Task When_ManyUpdates_Then_DataPreservedAfterCompaction()
	{
		const int count = 50;
		var (result, sut) = new StateImpl<int>(Context, Option.Some(0)).Record();

		for (var i = 1; i <= count; i++)
		{
			await sut.UpdateAsync(_ => i, CT);
		}

		await result.WaitForMessages(count + 1, CT);
		result.Last().Current.Data.SomeOrDefault().Should().Be(count);

		// After compaction, updates must still work correctly
		await sut.UpdateAsync(_ => 999, CT);
		await result.WaitForMessages(count + 2, CT);

		result.Last().Current.Data.SomeOrDefault().Should().Be(999);
	}

	[TestMethod]
	public async Task When_EmptyStateManyUpdates_Then_Compacted()
	{
		const int count = 50;
		var (result, sut) = new StateImpl<string>(Context, Option<string>.None()).Record();

		for (var i = 0; i < count; i++)
		{
			await sut.UpdateAsync(_ => $"value-{i}", CT);
		}

		await result.WaitForMessages(count + 1, CT);
		var activeCount = GetActiveUpdatesCount(sut);
		activeCount.Should().BeLessOrEqualTo(1);

		result.Last().Current.Data.SomeOrDefault().Should().Be($"value-{count - 1}");
	}

	[TestMethod]
	public async Task When_UpdateAfterCompaction_Then_NewUpdateApplied()
	{
		var (result, sut) = new StateImpl<int>(Context, Option.Some(0)).Record();

		// Trigger enough updates to guarantee compaction has run
		for (var i = 1; i <= 10; i++)
		{
			await sut.UpdateAsync(_ => i, CT);
		}

		await result.WaitForMessages(11, CT);

		// Now add more updates after compaction
		for (var i = 100; i <= 105; i++)
		{
			await sut.UpdateAsync(_ => i, CT);
		}

		await result.WaitForMessages(17, CT);
		result.Last().Current.Data.SomeOrDefault().Should().Be(105);

		// Active updates should still be bounded
		var activeCount = GetActiveUpdatesCount(sut);
		activeCount.Should().BeLessOrEqualTo(1);
	}

	[TestMethod]
	public async Task When_UpdateByMessage_Then_CompactedCorrectly()
	{
		var (result, sut) = new StateImpl<string>(Context, Option.Some("initial")).Record();

		await sut.UpdateMessageAsync(msg => msg.Data("updated-1"), CT);
		await sut.UpdateMessageAsync(msg => msg.Data("updated-2"), CT);
		await sut.UpdateMessageAsync(msg => msg.Data("updated-3"), CT);

		await result.WaitForMessages(4, CT);

		result.Last().Current.Data.SomeOrDefault().Should().Be("updated-3");

		var activeCount = GetActiveUpdatesCount(sut);
		activeCount.Should().BeLessOrEqualTo(1);
	}

	[TestMethod]
	public async Task When_ListStateValueManyAdds_Then_Compacted()
	{
		const int count = 50;
		var sut = ListState.Value(this, () => ImmutableList<int>.Empty);
		var result = sut.Record();

		for (var i = 0; i < count; i++)
		{
			await sut.AddAsync(i, CT);
		}

		await result.WaitForMessages(count + 1, CT);
		var finalData = result.Last().Current.Data.SomeOrDefault();
		finalData.Should().NotBeNull();
		finalData!.Count.Should().Be(count);

		// ListState.Value uses ValueFeed under the hood, compaction should be active
		var activeCount = GetActiveUpdatesCountForListState(sut);
		activeCount.Should().BeLessOrEqualTo(1,
			$"_activeUpdates should be compacted for ListState.Value, " +
			$"but {activeCount} Update records remain");
	}
	#endregion

	#region Helpers
	private static int GetActiveUpdatesCount<T>(IState<T> sut)
	{
		var stateImpl = sut as StateImpl<T>;
		Assert.IsNotNull(stateImpl, $"Expected StateImpl<{typeof(T).Name}> but got {sut.GetType().Name}");

		var innerField = typeof(StateImpl<T>).GetField("_inner", BindingFlags.Instance | BindingFlags.NonPublic)!;
		var updateFeed = innerField.GetValue(stateImpl)!;
		var count = FindFieldRecursive(updateFeed, "_activeUpdates", maxDepth: 15)
			?? FindFieldRecursive(SourceContext.Current, "_activeUpdates", maxDepth: 15);

		Assert.IsTrue(count.HasValue, "Could not find _activeUpdates via reflection");
		return count.Value;
	}

	private static int GetActiveUpdatesCountForListState<T>(IListState<T> sut)
	{
		var count = FindFieldRecursive(sut, "_activeUpdates", maxDepth: 15);
		if (!count.HasValue)
		{
			count = FindFieldRecursive(SourceContext.Current, "_activeUpdates", maxDepth: 15);
		}

		Assert.IsTrue(count.HasValue, "Could not find _activeUpdates via reflection for ListState");
		return count.Value;
	}

	private static int? FindFieldRecursive(
		object? root,
		string targetFieldName,
		int maxDepth,
		int depth = 0,
		HashSet<object>? visited = null)
	{
		if (root is null || depth > maxDepth)
		{
			return null;
		}

		visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
		if (!visited.Add(root))
		{
			return null;
		}

		var type = root.GetType();

		var field = type.GetField(targetFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (field is not null)
		{
			var value = field.GetValue(root);
			if (value is not null)
			{
				var countProp = value.GetType().GetProperty("Count");
				if (countProp is not null && countProp.PropertyType == typeof(int))
				{
					return (int)countProp.GetValue(value)!;
				}
			}
		}

		foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
		{
			if (f.FieldType.IsPrimitive || f.FieldType == typeof(string) || f.FieldType.IsEnum
				|| f.FieldType == typeof(global::System.Threading.CancellationToken)
				|| f.FieldType == typeof(global::System.Threading.CancellationTokenSource))
			{
				continue;
			}

			try
			{
				var val = f.GetValue(root);
				if (val is null)
				{
					continue;
				}

				if (val is global::System.Collections.IEnumerable enumerable and not string and not Array)
				{
					foreach (var item in enumerable)
					{
						var r = FindFieldRecursive(item, targetFieldName, maxDepth, depth + 1, visited);
						if (r.HasValue)
						{
							return r;
						}
					}
				}

				var result = FindFieldRecursive(val, targetFieldName, maxDepth, depth + 1, visited);
				if (result.HasValue)
				{
					return result;
				}
			}
			catch
			{
				// Skip
			}
		}

		return null;
	}
	#endregion
}
