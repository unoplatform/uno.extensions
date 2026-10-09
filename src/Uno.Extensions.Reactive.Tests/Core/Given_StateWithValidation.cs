using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Validation;

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
		// Explicit return type: a throw-only lambda also converts to the AsyncFunc<T, string?> overload.
		_ = state.Validate(ValueTask<IEnumerable<ValidationResult>> (string _, CancellationToken _) =>
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

	[TestMethod]
	public async Task When_MessageValidator_Then_SingleStateLevelResult_And_ClearedWhenValid()
	{
		var state = new StateImpl<string>(Context, Option.Some(""));
		_ = state.Validate(async (value, ct) => string.IsNullOrEmpty(value) ? "required" : null).Should().BeSameAs(state);

		await WaitForValidation(state, "required");
		state.Current.Current.Validation.Should().ContainSingle().Which.MemberNames.Should().BeEmpty();

		await state.SetAsync("value", CT);
		await WaitFor(() => state.Current.Current.Validation.Count is 0);
	}

	[TestMethod]
	public async Task When_PredicateValidator_Then_FixedMessage_And_ClearedWhenValid()
	{
		var state = new StateImpl<string>(Context, Option.Some(""));
		_ = state.Validate(async (value, ct) => !string.IsNullOrEmpty(value), "required").Should().BeSameAs(state);

		await WaitForValidation(state, "required");
		state.Current.Current.Validation.Should().ContainSingle().Which.MemberNames.Should().BeEmpty();

		await state.SetAsync("value", CT);
		await WaitFor(() => state.Current.Current.Validation.Count is 0);
	}

	[TestMethod]
	public async Task When_KeyValidator_Then_Localized()
	{
		var localizer = new TestLocalizer { { "Validation_Required", "requis" } };
		var state = new StateImpl<string>(Context, Option.Some(""));
		_ = state.Validate(async (value, ct) => string.IsNullOrEmpty(value) ? "Validation_Required" : null, localizer).Should().BeSameAs(state);

		await WaitForValidation(state, "requis");
		state.Current.Current.Validation.Should().ContainSingle().Which.MemberNames.Should().BeEmpty();

		await state.SetAsync("value", CT);
		await WaitFor(() => state.Current.Current.Validation.Count is 0);
	}

	[TestMethod]
	public async Task When_KeyNotFound_Then_KeyUsedAsMessage()
	{
		var state = new StateImpl<string>(Context, Option.Some(""));
		_ = state.Validate(async (value, ct) => !string.IsNullOrEmpty(value), "Validation.Required", new TestLocalizer());

		await WaitForValidation(state, "Validation.Required");
	}

	[TestMethod]
	public async Task When_PredicateKeyValidator_Then_LocalizedAtValidationTime()
	{
		var localizer = new TestLocalizer { { "Validation_Required", "first" } };
		var state = new StateImpl<string>(Context, Option.Some("invalid"));
		_ = state.Validate(async (value, ct) => value is not "invalid", "Validation_Required", localizer).Should().BeSameAs(state);
		await WaitForValidation(state, "first");

		localizer.Add("Validation_Required", "second");
		await state.SetAsync("valid", CT);
		await WaitFor(() => state.Current.Current.Validation.Count is 0);
		await state.SetAsync("invalid", CT);

		await WaitForValidation(state, "second");
	}

	[TestMethod]
	public async Task When_LocalizedListValidator_Then_KeysLocalized_MemberNamesKept()
	{
		var localizer = new TestLocalizer { { "Validation_Found", "localized" } };
		var state = new StateImpl<string>(Context, Option.Some("value"));

		_ = state.Validate(
			async (value, ct) => [new ValidationResult("Validation_Found", ["First"]), new ValidationResult("Not a key.", ["Second"])],
			localizer).Should().BeSameAs(state);

		await WaitFor(() => state.Current.Current.Validation.Count is 2);
		state.Current.Current.Validation.Select(result => (result.ErrorMessage, string.Join(",", result.MemberNames)))
			.Should()
			.Equal(("localized", "First"), ("Not a key.", "Second"));
	}

	[TestMethod]
	public async Task When_LocalizedListValidator_Then_LocalizerCalledOncePerResult()
	{
		var localizer = new TestLocalizer { { "Validation_A", "a" }, { "Validation_B", "b" } };
		var state = new StateImpl<string>(Context, Option.Some("value"));

		_ = state.Validate(async (value, ct) => [new ValidationResult("Validation_A"), new ValidationResult("Validation_B")], localizer);

		await WaitFor(() => state.Current.Current.Validation.Count is 2);
		_ = state.Current.Current.Validation.Select(result => result.ErrorMessage).ToList();
		_ = state.Current.Current.Validation.Select(result => result.ErrorMessage).ToList();

		localizer.Lookups.Should().Be(2);
	}

	[TestMethod]
	public async Task When_LocalizedValueHasBraces_Then_NotFormatted()
	{
		var localizer = new TestLocalizer { { "Validation_Required", "{0} est requis" } };
		var state = new StateImpl<string>(Context, Option.Some(""));

		_ = state.Validate(async (value, ct) => !string.IsNullOrEmpty(value), "Validation_Required", localizer);

		await WaitForValidation(state, "{0} est requis");
	}

	[TestMethod]
	public async Task When_NullLocalizer_Then_MessagesUnchanged()
	{
		var state = new StateImpl<string>(Context, Option.Some(""));

		_ = state.Validate(async (value, ct) => !string.IsNullOrEmpty(value), "Validation_Required", localizer: null);

		await WaitForValidation(state, "Validation_Required");
	}

	[TestMethod]
	public async Task When_OverloadRegisteredAfterValidator_Then_Replaced()
	{
		var validator = new TestValidator();
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(validator.Validate);
		(await validator.WaitForCall(0)).Complete(Error("initial"));
		await WaitForValidation(state, "initial");

		_ = state.Validate(async (value, ct) => false, "overload");
		await state.SetAsync("updated", CT);

		await WaitForValidation(state, "overload");
		validator.Calls.Should().HaveCount(1);
	}

	[TestMethod]
	public void When_InvalidArguments_Then_Throws()
	{
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		var localizer = new TestLocalizer();

		state.Invoking(s => s.Validate(default(Func<string, CancellationToken, ValueTask<IEnumerable<ValidationResult>>>)!, localizer)).Should().Throw<ArgumentNullException>();
		state.Invoking(s => s.Validate(default(AsyncFunc<string, string?>)!, localizer)).Should().Throw<ArgumentNullException>();
		state.Invoking(s => s.Validate(default(AsyncFunc<string, bool>)!, "error", localizer)).Should().Throw<ArgumentNullException>();
		state.Invoking(s => s.Validate(async (_, _) => true, "", localizer)).Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public async Task When_LocalizedIValidator_Then_DataAnnotationsKeysLocalized()
	{
		using var host = new HostBuilder()
			.UseValidation()
			.Build();
		var validator = host.Services.GetRequiredService<IValidator>();
		var localizer = new TestLocalizer { { "Validation_NameRequired", "Le nom est requis" } };
		var state = new StateImpl<Person>(Context, Option.Some(new Person()));

		_ = state.Validate((person, ct) => validator.ValidateAsync(person, null, ct), localizer);

		await WaitFor(() => state.Current.Current.Validation.Any(result => result.ErrorMessage == "Le nom est requis"));
		state.Current.Current.Validation.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(Person.Name));
	}

	[TestMethod]
	public async Task When_ValidatorReturnsSuccess_Then_Ignored()
	{
		var state = new StateImpl<string>(Context, Option.Some("initial"));

		_ = state.Validate(async (value, ct) => new[] { ValidationResult.Success!, new ValidationResult("error") });

		await WaitForValidation(state, "error");
		state.Current.Current.Validation.Should().ContainSingle().Which.ErrorMessage.Should().Be("error");
	}

	[TestMethod]
	public async Task When_IValidator_Then_ResultsPublished()
	{
		using var host = new HostBuilder()
			.UseValidation()
			.Build();
		var validator = host.Services.GetRequiredService<IValidator>();
		var state = new StateImpl<Person>(Context, Option.Some(new Person()));

		_ = state.Validate(validator).Should().BeSameAs(state);

		await WaitFor(() => state.Current.Current.Validation.Any(result => result.ErrorMessage == "Validation_NameRequired"));
		state.Current.Current.Validation.Should().ContainSingle().Which.MemberNames.Should().Equal(nameof(Person.Name));

		await state.UpdateAsync(_ => new Person { Name = "John" }, CT);
		await WaitFor(() => state.Current.Current.Validation.Count == 0);
	}

	[TestMethod]
	public async Task When_LocalizedIValidatorOverload_Then_KeysLocalized()
	{
		using var host = new HostBuilder()
			.UseValidation()
			.Build();
		var validator = host.Services.GetRequiredService<IValidator>();
		var localizer = new TestLocalizer { { "Validation_NameRequired", "Le nom est requis" } };
		var state = new StateImpl<Person>(Context, Option.Some(new Person()));

		_ = state.Validate(validator, localizer);

		await WaitFor(() => state.Current.Current.Validation.Any(result => result.ErrorMessage == "Le nom est requis"));
	}

	[TestMethod]
	public void When_NullIValidator_Then_Throws()
	{
		var state = new StateImpl<Person>(Context, Option.Some(new Person()));

		state.Invoking(s => s.Validate(default(IValidator)!)).Should().Throw<ArgumentNullException>();
	}

	public sealed class Person
	{
		[Required(ErrorMessage = "Validation_NameRequired")]
		public string? Name { get; set; }
	}

	private sealed class TestLocalizer : IStringLocalizer, IEnumerable<KeyValuePair<string, string>>
	{
		private readonly ConcurrentDictionary<string, string> _resources = new();
		private int _lookups;

		public int Lookups => _lookups;

		public void Add(string name, string value)
			=> _resources[name] = value;

		public LocalizedString this[string name]
		{
			get
			{
				Interlocked.Increment(ref _lookups);
				return _resources.TryGetValue(name, out var value)
					? new LocalizedString(name, value)
					: new LocalizedString(name, name, resourceNotFound: true);
			}
		}

		IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
			=> _resources.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
			=> _resources.GetEnumerator();

		public LocalizedString this[string name, params object[] arguments]
			=> new(name, string.Format(CultureInfo.CurrentCulture, this[name].Value, arguments));

		public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
			=> _resources.Select(kvp => new LocalizedString(kvp.Key, kvp.Value));
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
