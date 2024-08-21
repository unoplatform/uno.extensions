using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Messaging;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Extensions;

[TestClass]
public class Given_StateForEach : FeedTests
{
	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))] // Note: This is a compilation tests!
	public async Task When_ForEachAsync_Then_AcceptsNotNullAndStruct()
	{
		_ = default(IState<int>)!.ForEach(async (i, ct) => this.ToString());
		_ = default(IState<int?>)!.ForEach(async (i, ct) => this.ToString());
		_ = default(IState<string>)!.ForEach(async (i, ct) => this.ToString());
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		_ =  default(IState<string?>)!.ForEach(async (i, ct) => this.ToString());
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore
		_ = default(IState<MyStruct>)!.ForEach(async (i, ct) => this.ToString());
		_ = default(IState<MyStruct?>)!.ForEach(async (i, ct) => this.ToString());
		_ = default(IState<MyClass>)!.ForEach(async (i, ct) => this.ToString());
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		_ = default(IState<MyClass?>)!.ForEach(async (i, ct) => this.ToString());
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore
	}

	[TestMethod]
	[ExpectedException(typeof(ArgumentNullException))] // Note: This is a compilation tests!
	public async Task When_ForEachDataAsync_Then_AcceptsNotNullAndStruct()
	{
		_ = default(IState<int>)!.ForEach(async (i, ct) => this.ToString());
		default(IState<int?>)!.ForEachDataAsync(async (i, ct) => this.ToString());
		default(IState<string>)!.ForEachDataAsync(async (i, ct) => this.ToString());
		default(IState<string?>)!.ForEachDataAsync(async (i, ct) => this.ToString());
		default(IState<MyStruct>)!.ForEachDataAsync(async (i, ct) => this.ToString());
		default(IState<MyStruct?>)!.ForEachDataAsync(async (i, ct) => this.ToString());
		default(IState<MyClass>)!.ForEachDataAsync(async (i, ct) => this.ToString());
		default(IState<MyClass?>)!.ForEachDataAsync(async (i, ct) => this.ToString());
	}

	[TestMethod]
	public async Task When_UpdateState_Then_CallbackInvokedIgnoringInitialValue()
	{
		var state = State.Value(this, () => 1);
		var result = new List<int>();

		_ = state.ForEach(async (i, ct) => result.Add(i));

		await state.SetAsync(2, CT);
		await state.SetAsync(3, CT);
		await state.SetAsync(4, CT);

		result.Should().BeEquivalentTo(new[] { 2, 3, 4 });
	}

	[TestMethod]
	public async Task When_Fluent_UpdateState_Then_CallbackInvokedIgnoringInitialValue()
	{
		var result = new List<int>();

		var state = State.Async(this, async ct => 1)
						 .ForEach(async (i, ct) => result.Add(i));

		await state.SetAsync(2, CT);
		await state.SetAsync(3, CT);
		await state.SetAsync(4, CT);

		result.Should().BeEquivalentTo(new[] { 2, 3, 4 });
	}

	[TestMethod]
	public async Task When_Fluent_Multiple_UpdateState_Then_CallbackInvokedIgnoringInitialValue()
	{
		var result = new List<int>();

		ValueTask UpdateResultAsync(int i, CancellationToken ct)
		{
			result.Add(i);

			return ValueTask.CompletedTask;
		}

		var state = State.Async(this, async ct => 1)
						 .ForEach(UpdateResultAsync)
						 .ForEach(UpdateResultAsync);

		await state.SetAsync(2, CT);
		await state.SetAsync(3, CT);
		await state.SetAsync(4, CT);

		result.Should().BeEquivalentTo(new[] { 2, 3, 4 });
	}

	[TestMethod]
	public async Task When_Mixed_Multiple_UpdateState_Then_CallbackInvokedIgnoringInitialValue()
	{
		var result = new List<int>();

		ValueTask UpdateResultAsync(int i, CancellationToken ct)
		{
			result.Add(i);

			return ValueTask.CompletedTask;
		}

		var state = State.Async(this, async ct => 1)
						 .ForEach(UpdateResultAsync);
						 
		await state.ForEach(UpdateResultAsync);

		await state.SetAsync(2, CT);
		await state.SetAsync(3, CT);
		await state.SetAsync(4, CT);

		result.Should().BeEquivalentTo(new[] { 2, 3, 4 });
	}

	[TestMethod]
	public async Task When_UpdateStateAndCallbackIsAsync_Then_CallsAreQueued()
	{
		var state = State.Value(this, () => 1);
		var result = new List<int>();
		var tcs1 = new TaskCompletionSource();
		var tcs2 = new TaskCompletionSource();

		_ = state.ForEach(async (i, ct) =>
		{
			await (i switch
			{
				2 => tcs1.Task,
				3 => tcs2.Task,
				_ => throw new TestException()
			});
			result.Add(i);
		});

		await state.SetAsync(2, CT);
		await state.SetAsync(3, CT);

		tcs1.SetResult();
		result.Should().BeEquivalentTo(new[] { 2 });

		tcs2.SetResult();
		result.Should().BeEquivalentTo(new[] { 2, 3 });
	}

	[TestMethod]
	public async Task When_UpdateStateAndCallbackFails_Then_CallbackInvokedOnNextUpdate()
	{
		var state = State.Value(this, () => 1);
		var result = new List<int>();

		await state.ForEach(async (i, ct) =>
		{
			if (i is 42)
			{
				throw new TestException();
			}
			result.Add(i);
		});

		await state.SetAsync(2, CT);
		await state.SetAsync(42, CT);
		await state.SetAsync(3, CT);

		result.Should().BeEquivalentTo(new[] { 2, 3 });
	}

	[TestMethod]
	public async Task When_DisposeState_Then_EnumerationStop()
	{
		var state = State.Value(this, () => 1);
		await state.ForEach(async (i, ct) => this.ToString(), out var sut);

		await state.DisposeAsync();

		var enumerationTask = sut.GetType().GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(sut) as Task;
		if (enumerationTask is null)
		{
			Assert.Fail("Unable to get the private _task field of the StateListener<T>.");
		}

		enumerationTask.Status.Should().Be(TaskStatus.RanToCompletion);
	}

	[TestMethod]
	public async Task When_Fluent_DisposeState_Then_EnumerationStop()
	{
		var state = State.Value(this, () => 1)
						 .ForEach(async (i, ct) => this.ToString(), out var sut);

		await state.DisposeAsync();

		var enumerationTask = sut.GetType().GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(sut) as Task;
		if (enumerationTask is null)
		{
			Assert.Fail("Unable to get the private _task field of the StateListener<T>.");
		}

		enumerationTask.Status.Should().Be(TaskStatus.RanToCompletion);
	}

	[TestMethod]
	public async Task When_DisposeExecute_Then_EnumerationStop()
	{
		var state = State.Value(this, () => 1);
		await state.ForEach(async (i, ct) => this.ToString(), out var sut);

		sut.Dispose();

		await state.SetAsync(42, CT);

		var enumerationTask = sut.GetType().GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(sut) as Task;
		if (enumerationTask is null)
		{
			Assert.Fail("Unable to get the private _task field of the StateListener<T>.");
		}

		enumerationTask.Status.Should().Be(TaskStatus.RanToCompletion);
	}

	[TestMethod]
	public async Task When_Fluent_And_DisposeExecute_Then_EnumerationStop()
	{
		var state = State.Value(this, () => 1)
						  .ForEach(async (i, ct) => this.ToString(), out var sut);

		sut.Dispose();

		await state.SetAsync(42, CT);

		var enumerationTask = sut.GetType().GetField("_task", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(sut) as Task;
		if (enumerationTask is null)
		{
			Assert.Fail("Unable to get the private _task field of the StateListener<T>.");
		}

		enumerationTask.Status.Should().Be(TaskStatus.RanToCompletion);
	}

	[TestMethod]
	public async Task When_UpdateListStateWithNone_Then_CallbackGetsEmptyList()
	{
		var state = ListState.Value(this, () => ImmutableList.Create(1, 2, 3));
		var result = new List<IImmutableList<int>>();

		state.ForEachAsync(async (list, ct) => result.Add(list));

		await state.UpdateDataAsync(_ => Option.None<IImmutableList<int>>(), CT);

		result.Single().Should().NotBeNull().And.BeEquivalentTo(ImmutableList<int>.Empty);
	}

	private record class MyClass;
	private record struct MyStruct;
}
