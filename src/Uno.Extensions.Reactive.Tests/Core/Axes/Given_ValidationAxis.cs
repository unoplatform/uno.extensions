using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core.Axes;

[TestClass]
public class Given_ValidationAxis : FeedTests
{
	[TestMethod]
	public async Task When_SetValidation_Then_ValueIsKept_And_ResultsAreSet()
	{
		var result = new ValidationResult("Required", new[] { "Name" });
		var (recorder, sut) = new StateImpl<string>(Context, Option.Some("value")).Record();

		await sut.UpdateMessageAsync(msg => msg.Validation(new[] { result }), CT);
		await recorder.WaitForMessages(2);

		var msg = recorder.Last();
		msg.Changes.Contains(MessageAxis.Validation).Should().BeTrue();
		msg.Changes.Contains(MessageAxis.Data).Should().BeFalse();
		msg.Current.Data.SomeOrDefault().Should().Be("value");
		msg.Current.Error.Should().BeNull();
		msg.Current.Validation.Should().BeEquivalentTo(new[] { result });
	}

	[TestMethod]
	public async Task When_NoValidation_Then_EmptyAndNotSet()
	{
		var (recorder, sut) = new StateImpl<string>(Context, Option.Some("value")).Record();

		await recorder.WaitForMessages(1);

		var entry = recorder.Last().Current;
		entry.Validation.Should().BeEmpty();
		entry.Values.ContainsKey(MessageAxis.Validation).Should().BeFalse();
	}

	[TestMethod]
	[DataRow(true, DisplayName = "null")]
	[DataRow(false, DisplayName = "empty")]
	public async Task When_ClearValidation_Then_Unset(bool useNull)
	{
		var (recorder, sut) = new StateImpl<string>(Context, Option.Some("value")).Record();

		await sut.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await sut.UpdateMessageAsync(msg => msg.Validation(useNull ? null : Array.Empty<ValidationResult>()), CT);
		await recorder.WaitForMessages(3);

		var msg = recorder.Last();
		msg.Changes.Contains(MessageAxis.Validation).Should().BeTrue();
		msg.Current.Validation.Should().BeEmpty();
		msg.Current.Values.ContainsKey(MessageAxis.Validation).Should().BeFalse();
	}

	[TestMethod]
	public async Task When_SetIdenticalResults_Then_NoChange()
	{
		var (recorder, sut) = new StateImpl<string>(Context, Option.Some("value")).Record();

		await sut.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required", new[] { "Name" }) }), CT);
		await sut.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required", new[] { "Name" }) }), CT);
		await sut.UpdateMessageAsync(msg => msg.Data("updated"), CT); // Used as a marker, so we know that the previous update has been processed.
		await recorder.WaitForMessages(3);
		await Task.Yield();

		recorder.Count.Should().Be(3);
		recorder[1].Changes.Contains(MessageAxis.Validation).Should().BeTrue();
		recorder[2].Changes.Contains(MessageAxis.Validation).Should().BeFalse();
		recorder[2].Changes.Contains(MessageAxis.Data).Should().BeTrue();
	}

	[TestMethod]
	public async Task When_SetDifferentResults_Then_Changed()
	{
		var (recorder, sut) = new StateImpl<string>(Context, Option.Some("value")).Record();

		await sut.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required", new[] { "Name" }) }), CT);
		await sut.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required", new[] { "Other" }) }), CT);
		await recorder.WaitForMessages(3);

		recorder[2].Changes.Contains(MessageAxis.Validation).Should().BeTrue();
		recorder[2].Current.Validation.Single().MemberNames.Should().BeEquivalentTo("Other");
	}

	[TestMethod]
	public void When_Aggregate_Then_Concat()
	{
		var r1 = new ValidationResult("1");
		var r2 = new ValidationResult("2");
		var r3 = new ValidationResult("3");

		var aggregated = MessageAxis.Validation.Aggregate(new[]
		{
			MessageAxis.Validation.ToMessageValue(ImmutableList.Create(r1, r2)),
			MessageAxisValue.Unset,
			MessageAxis.Validation.ToMessageValue(ImmutableList.Create(r3)),
		});

		MessageAxis.Validation.FromMessageValue(aggregated).Should().Equal(r1, r2, r3);
	}

	[TestMethod]
	public async Task When_Select_Then_ValidationNotPropagated()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var recorder = state.Select(value => value + "!").Record();

		await state.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await state.UpdateMessageAsync(msg => msg.Data("updated"), CT); // Marker
		await recorder.WaitForMessages(2);
		await Task.Yield();

		recorder.Count.Should().Be(2);
		recorder.Should().AllSatisfy(msg => msg.Current.Validation.Should().BeEmpty());
		recorder.Last().Current.Data.SomeOrDefault().Should().Be("updated!");
	}

	[TestMethod]
	public async Task When_SelectAsync_Then_ValidationNotPropagated()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var recorder = state.SelectAsync(async (value, _) => value + "!").Record();

		await state.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await state.UpdateMessageAsync(msg => msg.Data("updated"), CT); // Marker
		await recorder.WaitForMessages(2);
		await Task.Yield();

		recorder.Should().AllSatisfy(msg => msg.Current.Validation.Should().BeEmpty());
		recorder.Last().Current.Data.SomeOrDefault().Should().Be("updated!");
	}

	[TestMethod]
	public async Task When_Where_Then_ValidationNotPropagated()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var recorder = state.Where(_ => true).Record();

		await state.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await state.UpdateMessageAsync(msg => msg.Data("updated"), CT); // Marker
		await recorder.WaitForMessages(2);
		await Task.Yield();

		recorder.Count.Should().Be(2);
		recorder.Should().AllSatisfy(msg => msg.Current.Validation.Should().BeEmpty());
	}

	[TestMethod]
	public async Task When_Combine_Then_ValidationNotPropagated()
	{
		var state1 = new StateImpl<string>(Context, Option.Some("a"));
		var state2 = new StateImpl<string>(Context, Option.Some("b"));
		var recorder = Feed.Combine(state1, state2).Record();

		await state1.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await state2.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await state1.UpdateMessageAsync(msg => msg.Data("updated"), CT); // Marker
		await recorder.WaitForMessages(2);
		await Task.Yield();

		recorder.Count.Should().Be(2);
		recorder.Should().AllSatisfy(msg => msg.Current.Validation.Should().BeEmpty());
		recorder.Last().Current.Data.SomeOrDefault().Should().Be(("updated", "b"));
	}

	[TestMethod]
	public async Task When_DynamicFeed_Then_ValidationNotPropagated()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var recorder = Feed.Dynamic(async ct => await state + "!").Record();

		await state.UpdateMessageAsync(msg => msg.Validation(new[] { new ValidationResult("Required") }), CT);
		await state.UpdateMessageAsync(msg => msg.Data("updated"), CT); // Marker
		await recorder.WaitForMessages(2);
		await Task.Yield();

		recorder.Should().AllSatisfy(msg => msg.Current.Validation.Should().BeEmpty());
		recorder.Last().Current.Data.SomeOrDefault().Should().Be("updated!");
	}

	[TestMethod]
	public async Task When_StateOfState_Then_ParentValidationForwarded_And_LocalReplacesParent()
	{
		// A state is the same logical feed as its source (e.g. hot-reload swaps the source of a state with another state),
		// so the validation of the source is kept, but local results replace (and are not merged with) the parent ones.
		var parent = new StateImpl<string>(Context, Option.Some("value"));
		var (recorder, sut) = new StateImpl<string>(Context, parent).Record();
		var parentResult = new ValidationResult("Parent");
		var localResult = new ValidationResult("Local");

		await parent.UpdateMessageAsync(msg => msg.Validation(new[] { parentResult }), CT);
		await WaitFor(() => recorder.LastOrDefault()?.Current.Validation.Contains(parentResult) ?? false);

		await sut.UpdateMessageAsync(msg => msg.Validation(new[] { localResult }), CT);
		await WaitFor(() => recorder.LastOrDefault()?.Current.Validation.Contains(localResult) ?? false);

		recorder.Last().Current.Validation.Should().Equal(localResult);
	}

	private static async Task WaitFor(Func<bool> predicate)
	{
		for (var i = 0; i < 500; i++)
		{
			if (predicate())
			{
				return;
			}

			await Task.Delay(10);
		}

		throw new TimeoutException();
	}
}
