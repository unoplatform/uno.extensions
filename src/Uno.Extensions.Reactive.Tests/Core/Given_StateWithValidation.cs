using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_StateWithValidation : FeedTests
{
	[TestMethod]
	public async Task When_Validate_Then_InitialValueValidated()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));

		_ = state.Validate(validator.Validate).Should().BeSameAs(state);

		var call = await validator.WaitForCall(0);
		call.Value.Should().Be("initial");
		call.Complete(Error("initial"));

		await WaitForValidation(state, "initial");
		state.Current.Current.Data.SomeOrDefault().Should().Be("initial");
		state.Current.Current.Error.Should().BeNull();
	}

	[TestMethod]
	public async Task When_DataChanged_Then_Revalidated()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		(await validator.WaitForCall(0)).Complete(Error("initial"));
		await WaitForValidation(state, "initial");

		await state.SetAsync("updated", CT);

		var call = await validator.WaitForCall(1);
		call.Value.Should().Be("updated");
		call.Complete(Error("updated"));
		await WaitForValidation(state, "updated");
	}

	[TestMethod]
	public async Task When_ResultsPublished_Then_NotRevalidated()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		(await validator.WaitForCall(0)).Complete(Error("initial"));
		await WaitForValidation(state, "initial");

		// Changing only the validation must not re-trigger the validator (only data changes does).
		await state.UpdateMessageAsync(msg => msg.Validation(Error("manual")), CT);
		await state.SetAsync("updated", CT); // Marker

		var call = await validator.WaitForCall(1);
		call.Value.Should().Be("updated");
		validator.Calls.Should().HaveCount(2);
	}

	[TestMethod]
	public async Task When_DataChangedWhileValidating_Then_PreviousCancelled()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		var first = await validator.WaitForCall(0);

		await state.SetAsync("updated", CT);

		var second = await validator.WaitForCall(1);
		first.Ct.IsCancellationRequested.Should().BeTrue();
		second.Ct.IsCancellationRequested.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_StaleResults_Then_Discarded()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		var first = await validator.WaitForCall(0);

		await state.SetAsync("updated", CT);
		var second = await validator.WaitForCall(1);

		// Validator does not honor the cancellation: results for the second value are published first, then the ones of the stale first value.
		second.Complete(Error("updated"));
		await WaitForValidation(state, "updated");
		first.Complete(Error("initial"));

		await state.SetAsync("updated2", CT); // Marker
		var third = await validator.WaitForCall(2);
		state.Current.Current.Validation.Should().ContainSingle().Which.ErrorMessage.Should().Be("updated");
		third.Value.Should().Be("updated2");
	}

	[TestMethod]
	public async Task When_ResultsForValueNoLongerCurrent_Then_Discarded()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		(await validator.WaitForCall(0)).Complete(Error("initial"));
		await WaitForValidation(state, "initial");

		await state.SetAsync("updated", CT);
		var second = await validator.WaitForCall(1);
		second.Complete(Error("updated"));
		await WaitForValidation(state, "updated");

		state.Current.Current.Validation.Should().ContainSingle().Which.ErrorMessage.Should().Be("updated");
	}

	[TestMethod]
	public async Task When_ValidatorThrows_Then_StateNotInError_And_PreviousResultsKept()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		(await validator.WaitForCall(0)).Complete(Error("initial"));
		await WaitForValidation(state, "initial");

		await state.SetAsync("updated", CT);
		(await validator.WaitForCall(1)).Fail(new InvalidOperationException("validator failed"));

		await state.SetAsync("updated2", CT); // Marker, makes sure the failure has been processed
		(await validator.WaitForCall(2)).Complete(Error("updated2"));
		await WaitForValidation(state, "updated2");

		state.Current.Current.Error.Should().BeNull();
		state.Current.Current.Data.SomeOrDefault().Should().Be("updated2");
	}

	[TestMethod]
	public async Task When_ValidatorThrowsSync_Then_StateNotInError()
	{
		var calls = 0;
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate((_, _) =>
		{
			Interlocked.Increment(ref calls);
			throw new InvalidOperationException("validator failed");
		});

		await WaitFor(() => calls is 1);
		await state.SetAsync("updated", CT);
		await WaitFor(() => calls is 2);

		state.Current.Current.Error.Should().BeNull();
		state.Current.Current.Data.SomeOrDefault().Should().Be("updated");
	}

	[TestMethod]
	public async Task When_RegisteredMultipleTimes_Then_LastValidatorUsed_And_NotStacked()
	{
		var validator1 = new TestValidator();
		var validator2 = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));

		_ = state.Validate(validator1.Validate);
		(await validator1.WaitForCall(0)).Complete(Error("initial"));
		await WaitForValidation(state, "initial");

		_ = state.Validate(validator2.Validate);
		_ = state.Validate(validator2.Validate);
		await state.SetAsync("updated", CT);

		(await validator2.WaitForCall(0)).Complete(Error("updated"));
		await WaitForValidation(state, "updated");

		validator1.Calls.Should().HaveCount(1);
		validator2.Calls.Should().HaveCount(1);
	}

	[TestMethod]
	public async Task When_None_Then_ResultsCleared_WithoutInvokingValidator()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		(await validator.WaitForCall(0)).Complete(Error("initial"));
		await WaitForValidation(state, "initial");

		await state.UpdateDataAsync(_ => Option<string>.None(), CT);

		await WaitFor(() => state.Current.Current.Validation.Count is 0);
		validator.Calls.Should().HaveCount(1);
	}

	[TestMethod]
	public async Task When_Undefined_Then_NotValidated()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option<string>.Undefined());
		_ = state.Validate(validator.Validate);

		await state.SetAsync("value", CT);

		var call = await validator.WaitForCall(0);
		call.Value.Should().Be("value");
	}

	[TestMethod]
	public async Task When_Disposed_Then_PendingValidationCancelled()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		var call = await validator.WaitForCall(0);

		await state.DisposeAsync();

		call.Ct.IsCancellationRequested.Should().BeTrue();
	}

	[TestMethod]
	public async Task When_ChangeFromBinding_Then_Validated()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		(await validator.WaitForCall(0)).Complete(Error("initial"));

		// Updates coming from the binding (like in BindableViewModelBase) must be validated too.
		await state.UpdateMessageAsync(msg => msg.Data("typed").Set(Uno.Extensions.Reactive.Bindings.BindableViewModelBase.BindingSource, "view"), CT);

		(await validator.WaitForCall(1)).Value.Should().Be("typed");
	}

	[TestMethod]
	public void When_NotAStateImpl_Then_Throws()
	{
		var state = new CustomState();

		state.Invoking(s => s.Validate((_, _) => new(Enumerable.Empty<ValidationResult>())))
			.Should()
			.Throw<NotSupportedException>();
	}

	private static ValidationResult[] Error(string message)
		=> new[] { new ValidationResult(message) };

	private async Task WaitForValidation(StateImpl<string> state, string errorMessage)
		=> await WaitFor(() => state.Current.Current.Validation.Any(result => result.ErrorMessage == errorMessage));

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

	private sealed class TestValidator
	{
		private readonly List<Call> _calls = new();

		public IReadOnlyList<Call> Calls
		{
			get
			{
				lock (_calls)
				{
					return _calls.ToList();
				}
			}
		}

		public ValueTask<IEnumerable<ValidationResult>> Validate(string value, CancellationToken ct)
		{
			var call = new Call(value, ct);
			lock (_calls)
			{
				_calls.Add(call);
			}

			return new(call.Result.Task);
		}

		public async Task<Call> WaitForCall(int index)
		{
			await WaitFor(() => Calls.Count > index);
			return Calls[index];
		}
	}

	private sealed record Call(string Value, CancellationToken Ct)
	{
		public TaskCompletionSource<IEnumerable<ValidationResult>> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Complete(IEnumerable<ValidationResult> results)
			=> Result.SetResult(results);

		public void Fail(Exception error)
			=> Result.SetException(error);
	}

	private sealed class CustomState : IState<string>
	{
		public SourceContext Context => SourceContext.None;

		IRequestSource IState.Requests => throw new NotSupportedException();

		public IAsyncEnumerable<Message<string>> GetSource(SourceContext context, CancellationToken ct = default)
			=> throw new NotSupportedException();

		public ValueTask UpdateMessageAsync(Action<MessageBuilder<string>> updater, CancellationToken ct)
			=> throw new NotSupportedException();

		public ValueTask DisposeAsync()
			=> default;
	}
}
