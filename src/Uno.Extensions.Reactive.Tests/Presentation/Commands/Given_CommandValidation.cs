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
using Uno.Extensions.Reactive.Commands;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Validation;
using static Uno.Extensions.Reactive.Tests.ValidationTestHelper;

namespace Uno.Extensions.Reactive.Tests.Commands;

[TestClass]
public class Given_CommandValidation : FeedUITests
{
	[TestMethod]
	public async Task When_Invalid_Then_NotExecuted_And_ResultsPublished()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => Error("invalid"))
			.Then(Execute));

		await sut.ExecuteAndWait();

		sut.Executions.Should().BeEmpty();
		sut.Errors.Should().BeEmpty();
		sut.Completions.Should().ContainSingle().Which.Error.Should().BeNull();
		Validation(state).Should().Equal("invalid");
	}

	[TestMethod]
	public async Task When_Valid_Then_Executed_And_ResultsCleared()
	{
		var state = new StateImpl<string>(Context, Option.Some("invalid"));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => value == "invalid" ? Error("invalid") : Valid())
			.Then(Execute));
		await sut.ExecuteAndWait();
		Validation(state).Should().Equal("invalid");

		await state.SetAsync("valid", CT);
		await sut.ExecuteAndWait();

		sut.Executions.Should().Equal("valid");
		Validation(state).Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_DataChangedAfterFailedExecution_Then_ResultsKept()
	{
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => Error($"invalid {value}"))
			.Then(Execute));
		await sut.ExecuteAndWait();

		await state.SetAsync("updated", CT);
		await state.SetAsync("updated2", CT);

		state.Current.Current.Data.SomeOrDefault().Should().Be("updated2");
		Validation(state).Should().Equal("invalid initial");
	}

	[TestMethod]
	public async Task When_ExecutedAgain_Then_Revalidated_And_ResultsReplaced()
	{
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		var validations = new List<string>();
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) =>
			{
				lock (validations)
				{
					validations.Add(value);
				}
				return Error($"invalid {value}");
			})
			.Then(Execute));
		await sut.ExecuteAndWait();

		await state.SetAsync("updated", CT);
		await sut.ExecuteAndWait();

		validations.Should().Equal("initial", "updated");
		Validation(state).Should().Equal("invalid updated");
		sut.Executions.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_ValidatorThrows_Then_ErrorReported_And_NotExecuted_And_PreviousResultsKept()
	{
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => value == "initial" ? Error("invalid") : throw new TestException())
			.Then(Execute));
		await sut.ExecuteAndWait();

		await state.SetAsync("updated", CT);
		await sut.ExecuteAndWait();

		sut.Executions.Should().BeEmpty();
		sut.Errors.Should().ContainSingle()
			.Which.InnerException.Should().BeOfType<InvalidOperationException>()
			.Which.InnerException.Should().BeOfType<TestException>();
		sut.Completions.Last().Error.Should().BeOfType<InvalidOperationException>()
			.Which.Message.Should().Contain("validation").And.NotContain("updated"); // No user input in the error
		Validation(state).Should().Equal("invalid");
	}

	[TestMethod]
	public async Task When_Validating_Then_IsExecuting_And_CannotExecuteAgain()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var validation = new TaskCompletionSource<IEnumerable<ValidationResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var sut = Create(b => b
			.Given(state)
			.Validation((value, ct) => new ValueTask<IEnumerable<ValidationResult>>(validation.Task))
			.Then(Execute));
		await sut.WaitForCanExecute();

		sut.Command.Execute(null);

		sut.Command.IsExecuting.Should().BeTrue();
		sut.Command.CanExecute(null).Should().BeFalse();

		validation.SetResult(Valid());
		await WaitFor(() => sut.Completions.Count == 1);

		sut.Executions.Should().Equal("value");
	}

	[TestMethod]
	public async Task When_DisposedWhileValidating_Then_ValidationCancelled()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var validationCt = new TaskCompletionSource<CancellationToken>();
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) =>
			{
				validationCt.SetResult(ct);
				await Task.Delay(Timeout.Infinite, ct);
				return Valid();
			})
			.Then(Execute));
		await sut.WaitForCanExecute();
		sut.Command.Execute(null);
		var ct = await validationCt.Task;

		sut.Command.Dispose();

		ct.IsCancellationRequested.Should().BeTrue();
	}

	[TestMethod]
	public async Task When_DisposedWhileValidatingAndValidatorIgnoresCancellation_Then_NothingPublished()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		// Note: Continuations are run synchronously, so the end of the execution runs within the SetResult below.
		var validation = new TaskCompletionSource<IEnumerable<ValidationResult>>();
		var sut = Create(b => b
			.Given(state)
			.Validation((value, ct) => new ValueTask<IEnumerable<ValidationResult>>(validation.Task))
			.Then(Execute));
		await sut.WaitForCanExecute();
		sut.Command.Execute(null);
		await WaitFor(() => sut.Starts == 1);

		sut.Command.Dispose();
		validation.SetResult(Error("invalid"));

		sut.Executions.Should().BeEmpty();
		Validation(state).Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_DisposedWhileValidatingAndValidatorIgnoresCancellation_Then_ActionNotInvoked()
	{
#pragma warning disable FEED2003 // A feed parameter (nothing published) makes sure that only the check before the action can prevent the execution.
		var feed = Feed.Async(async ct => "value");
		// Note: Continuations are run synchronously, so the end of the execution runs within the SetResult below.
		var validation = new TaskCompletionSource<IEnumerable<ValidationResult>>();
		var sut = Create(b => b
			.Given(feed)
			.Validation((value, ct) => new ValueTask<IEnumerable<ValidationResult>>(validation.Task))
			.Then(Execute));
#pragma warning restore FEED2003
		await sut.WaitForCanExecute();
		sut.Command.Execute(null);
		await WaitFor(() => sut.Starts == 1);

		sut.Command.Dispose();
		validation.SetResult(Valid());

		sut.Executions.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_GivenStateDisposed_Then_ExecutionCompletes()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => Error("invalid"))
			.Then(Execute));
		await sut.WaitForCanExecute();

		await state.DisposeAsync();
		await sut.ExecuteAndWait();

		sut.Executions.Should().BeEmpty();
		sut.Command.IsExecuting.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_CannotExecute_Then_NotValidated()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var canExecuteEvaluated = false;
		var validated = false;
		var sut = Create(b => b
			.Given(state)
			.When(value =>
			{
				canExecuteEvaluated = true;
				return false;
			})
			.Validation(async (value, ct) =>
			{
				validated = true;
				return Error("invalid");
			})
			.Then(Execute));
		sut.Command.CanExecute(null); // Make sure the command is initialized (i.e. subscribed to its parameter).
		await WaitFor(() => canExecuteEvaluated);

		sut.Command.Execute(null);

		// The command decides synchronously whether an execution starts, and the validation runs only in a started execution.
		sut.Starts.Should().Be(0);
		sut.Command.IsExecuting.Should().BeFalse();
		validated.Should().BeFalse();
		Validation(state).Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_ValidationAfterWhen_Then_Validated()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var sut = Create(b => b
			.Given(state)
			.When(value => value is { Length: > 0 })
			.Validation(async (value, ct) => Error("invalid"))
			.Then(Execute));

		await sut.ExecuteAndWait();

		sut.Executions.Should().BeEmpty();
		Validation(state).Should().Equal("invalid");
	}

	[TestMethod]
	public async Task When_SingleRule_Then_ErrorPublished()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => value.Length > 10 ? null : "too short")
			.Then(Execute));

		await sut.ExecuteAndWait();

		sut.Executions.Should().BeEmpty();
		var result = state.Current.Current.Validation.Should().ContainSingle().Subject;
		result.ErrorMessage.Should().Be("too short");
		result.MemberNames.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Predicate_Then_ErrorPublishedOnlyWhenNotValid()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => value.Length > 10, "too short")
			.Then(Execute));
		await sut.ExecuteAndWait();
		Validation(state).Should().Equal("too short");

		await state.SetAsync("a valid long value", CT);
		await sut.ExecuteAndWait();

		Validation(state).Should().BeEmpty();
		sut.Executions.Should().Equal("a valid long value");
	}

	[TestMethod]
	public async Task When_IValidator_Then_ResultsPublishedWithMemberNames()
	{
		using var host = new HostBuilder().UseValidation().Build();
		var validator = host.Services.GetRequiredService<IValidator>();
		var state = new StateImpl<ValidatedPerson>(Context, Option.Some(new ValidatedPerson()));
		var executed = 0;
		var sut = Create(b => b
			.Given(state)
			.Validation(validator)
			.Then(async (person, ct) => Interlocked.Increment(ref executed)));
		await sut.ExecuteAndWait();

		var result = state.Current.Current.Validation.Should().ContainSingle().Subject;
		result.ErrorMessage.Should().Be("Validation_NameRequired");
		result.MemberNames.Should().Equal(nameof(ValidatedPerson.Name));

		await state.UpdateAsync(_ => new ValidatedPerson { Name = "John" }, CT);
		await sut.ExecuteAndWait();

		executed.Should().Be(1);
		state.Current.Current.Validation.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_Localized_Then_KeysResolved()
	{
		using var host = new HostBuilder().UseValidation().Build();
		var validator = host.Services.GetRequiredService<IValidator>();
		var localizer = new TestLocalizer { { "Validation_NameRequired", "Le nom est requis" }, { "Validation_TooShort", "Trop court" } };
		var person = new StateImpl<ValidatedPerson>(Context, Option.Some(new ValidatedPerson()));
		var text = new StateImpl<string>(Context, Option.Some("value"));
		var personSut = Create(b => b.Given(person).Validation(validator, localizer).Then(async (_, _) => { }));
		var textSut = Create(b => b.Given(text).Validation(async (value, ct) => false, "Validation_TooShort", localizer).Then(Execute));

		await personSut.ExecuteAndWait();
		await textSut.ExecuteAndWait();

		person.Current.Current.Validation.Should().ContainSingle().Which.ErrorMessage.Should().Be("Le nom est requis");
		Validation(text).Should().Equal("Trop court");
	}

#pragma warning disable FEED2003 // Validation of a parameter which is not a state is the scenario under test.
	[TestMethod]
	public async Task When_ParameterIsNotState_Then_NotExecuted_And_NoError()
	{
		var feed = Feed.Async(async ct => "value");
		var sut = Create(b => b
			.Given(feed)
			.Validation(async (value, ct) => Error("invalid"))
			.Then(Execute));

		await sut.ExecuteAndWait();

		sut.Executions.Should().BeEmpty();
		sut.Errors.Should().BeEmpty();
		sut.Completions.Should().ContainSingle().Which.Error.Should().BeNull();
	}

	[TestMethod]
	public async Task When_ParameterIsNotStateAndValid_Then_Executed()
	{
		var feed = Feed.Async(async ct => "value");
		var sut = Create(b => b
			.Given(feed)
			.Validation(async (value, ct) => Valid())
			.Then(Execute));

		await sut.ExecuteAndWait();

		sut.Executions.Should().Equal("value");
	}

#pragma warning restore FEED2003

	[TestMethod]
	public async Task When_StateAlsoValidatedOnChange_Then_LastWriterWins()
	{
		var state = new StateImpl<string>(Context, Option.Some("initial"));
		_ = state.Validate(async (value, ct) => Error($"on change {value}"));
		await WaitFor(() => Validation(state).SequenceEqual(new[] { "on change initial" }));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => Error($"command {value}"))
			.Then(Execute));

		await sut.ExecuteAndWait();
		Validation(state).Should().Equal("command initial");

		await state.SetAsync("updated", CT);
		await WaitFor(() => Validation(state) is [not "command initial"]);
		Validation(state).Should().Equal("on change updated");
	}

	[TestMethod]
	public async Task When_SourceOfStateProducesNewValue_Then_ResultsCleared()
	{
		var refresh = new Signal();
		var state = new StateImpl<string>(Context, Feed<string>.Async(async ct => "value", refresh));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => Error("invalid"))
			.Then(Execute));
		await sut.ExecuteAndWait();
		Validation(state).Should().Equal("invalid");

		// Like any (volatile) update of a state, the results are dropped when its source produces a new value.
		refresh.Raise();

		await WaitFor(() => Validation(state).Length == 0);
	}

	[TestMethod]
	public async Task When_ValidatorCancelledByItself_Then_ExecutionFails()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var sut = Create(b => b
			.Given(state)
			.Validation(ValueTask<IEnumerable<ValidationResult>> (string value, CancellationToken ct) => throw new TaskCanceledException("e.g. timeout of an HttpClient"))
			.Then(Execute));

		await sut.ExecuteAndWait();

		sut.Executions.Should().BeEmpty();
		sut.Errors.Should().ContainSingle()
			.Which.InnerException.Should().BeOfType<InvalidOperationException>()
			.Which.InnerException.Should().BeOfType<TaskCanceledException>();
	}

	[TestMethod]
	public async Task When_ValidationConfiguredTwice_Then_LastWins()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var sut = Create(b => b
			.Given(state)
			.Validation(async (value, ct) => Error("first"))
			.Validation(async (value, ct) => Error("second"))
			.Then(Execute));

		await sut.ExecuteAndWait();

		Validation(state).Should().Equal("second");
	}

	[TestMethod]
	public async Task When_BuilderOfDerivedType_Then_ResultsPublished()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		var sut = Create(b =>
		{
			// The builder interfaces are covariant: the builder of the string state is used as a builder of object.
			ICommandBuilder<object> builder = b.Given(state);
			builder
				.Validation(async (object value, CancellationToken ct) => Error($"invalid {value}"))
				.Then(Execute);
		});

		await sut.ExecuteAndWait();

		sut.Executions.Should().BeEmpty();
		Validation(state).Should().Equal("invalid value");
	}

	[TestMethod]
	public async Task When_ParameterFromView_Then_Validated_And_Aborted()
	{
		var validated = new List<string>();
#pragma warning disable FEED2003 // Validation of a parameter provided by the view is the scenario under test.
		var sut = CreateFromView<string>(b => b
			.Validation(async (value, ct) =>
			{
				lock (validated)
				{
					validated.Add(value);
				}
				return value == "valid" ? Valid() : Error("invalid");
			})
			.Then(Execute));
#pragma warning restore FEED2003

		await sut.ExecuteAndWait("invalid");
		await sut.ExecuteAndWait("valid");

		validated.Should().Equal("invalid", "valid");
		sut.Executions.Should().Equal("valid");
		sut.Errors.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_NullParameterFromViewWithIValidator_Then_ConsideredValid()
	{
		using var host = new HostBuilder().UseValidation().Build();
		var validator = host.Services.GetRequiredService<IValidator>();
#pragma warning disable FEED2003 // Validation of a parameter provided by the view is the scenario under test.
		var sut = CreateFromView<ValidatedPerson>(b => b
			.Validation(validator)
			.Then(Execute));
#pragma warning restore FEED2003

		await sut.ExecuteAndWait(null);

		sut.Executions.Should().Equal(new object?[] { null });
	}

	[TestMethod]
	public void When_InvalidArguments_Then_Throws()
	{
		var state = new StateImpl<string>(Context, Option.Some("value"));
		ICommandBuilder root = new CommandBuilder<object>("sut");
		var builder = root.Given(state);
		var conditional = builder.When(_ => true);

		builder.Invoking(b => b.Validation(default(Func<string, CancellationToken, ValueTask<IEnumerable<ValidationResult>>>)!)).Should().Throw<ArgumentNullException>();
		builder.Invoking(b => b.Validation(default(AsyncFunc<string, string?>)!)).Should().Throw<ArgumentNullException>();
		builder.Invoking(b => b.Validation(default(AsyncFunc<string, bool>)!, "error")).Should().Throw<ArgumentNullException>();
		builder.Invoking(b => b.Validation(async (_, _) => true, "")).Should().Throw<ArgumentException>();
		builder.Invoking(b => b.Validation(default(IValidator)!)).Should().Throw<ArgumentNullException>();
		conditional.Invoking(b => b.Validation(default(Func<string, CancellationToken, ValueTask<IEnumerable<ValidationResult>>>)!)).Should().Throw<ArgumentNullException>();
		conditional.Invoking(b => b.Validation(default(AsyncFunc<string, string?>)!)).Should().Throw<ArgumentNullException>();
		conditional.Invoking(b => b.Validation(default(AsyncFunc<string, bool>)!, "error")).Should().Throw<ArgumentNullException>();
		conditional.Invoking(b => b.Validation(async (_, _) => true, "")).Should().Throw<ArgumentException>();
		conditional.Invoking(b => b.Validation(default(IValidator)!)).Should().Throw<ArgumentNullException>();
	}

	[TestMethod]
	public void When_CustomBuilder_Then_NotSupported()
	{
		new CustomBuilder()
			.Invoking(b => b.Validation(async (value, ct) => Valid()))
			.Should()
			.Throw<NotSupportedException>();
	}

	private SutCommand Create(Action<ICommandBuilder> build)
	{
		var errors = new List<Exception>();
		var builder = new CommandBuilder<object>("sut");
		build(builder);
		var command = builder.Build(Context, error =>
		{
			lock (errors)
			{
				errors.Add(error);
			}
		});

		return new SutCommand(command, errors, _executions);
	}

	private SutCommand CreateFromView<T>(Action<ICommandBuilder<T>> build)
	{
		var errors = new List<Exception>();
		var builder = new CommandBuilder<T>("sut");
		build(builder);
		var command = builder.Build(Context, error =>
		{
			lock (errors)
			{
				errors.Add(error);
			}
		});

		return new SutCommand(command, errors, _executions);
	}

	private readonly List<object?> _executions = new();

	private async ValueTask Execute<T>(T parameter, CancellationToken ct)
	{
		lock (_executions)
		{
			_executions.Add(parameter);
		}
	}

	private static string[] Validation<T>(StateImpl<T> state)
		=> state.Current.Current.Validation.Select(result => result.ErrorMessage ?? "").ToArray();

	private static IEnumerable<ValidationResult> Error(string message)
		=> new[] { new ValidationResult(message) };

	private static IEnumerable<ValidationResult> Valid()
		=> Array.Empty<ValidationResult>();

	private sealed class SutCommand
	{
		private readonly List<Exception> _errors;
		private readonly List<object?> _executions;
		private readonly List<ExecutionCompletedEventArgs> _completions = new();
		private int _starts;

		public SutCommand(IAsyncCommand command, List<Exception> errors, List<object?> executions)
		{
			Command = (AsyncCommand)command;
			_errors = errors;
			_executions = executions;

			Command.ExecutionStarted += (snd, args) => Interlocked.Increment(ref _starts);
			Command.ExecutionCompleted += (snd, args) =>
			{
				lock (_completions)
				{
					_completions.Add(args);
				}
			};
		}

		public AsyncCommand Command { get; }

		public int Starts => _starts;

		public IReadOnlyList<ExecutionCompletedEventArgs> Completions
		{
			get
			{
				lock (_completions)
				{
					return _completions.ToList();
				}
			}
		}

		public IReadOnlyList<Exception> Errors
		{
			get
			{
				lock (_errors)
				{
					return _errors.ToList();
				}
			}
		}

		public IReadOnlyList<object?> Executions
		{
			get
			{
				lock (_executions)
				{
					return _executions.ToList();
				}
			}
		}

		public async Task WaitForCanExecute(object? parameter = null)
			=> await WaitFor(() => Command.CanExecute(parameter));

		public async Task ExecuteAndWait(object? parameter = null)
		{
			await WaitForCanExecute(parameter);

			var count = Completions.Count;
			Command.Execute(parameter);
			await WaitFor(() => Completions.Count > count);
		}
	}

	private sealed class CustomBuilder : ICommandBuilder<string>
	{
		public IConditionalCommandBuilder<string> When(Predicate<string> canExecute) => throw new NotSupportedException();
		public void Then(AsyncAction<string> execute) => throw new NotSupportedException();
		public void Execute(AsyncAction<string> execute) => throw new NotSupportedException();
	}
}
