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
		sut.Errors.Should().ContainSingle().Which.InnerException.Should().BeOfType<TestException>();
		sut.Completions.Last().Error.Should().BeOfType<TestException>();
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
	public async Task When_DisposedWhileValidating_Then_ValidationCancelled_And_NotExecuted()
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

		((IDisposable)sut.Command).Dispose();

		ct.IsCancellationRequested.Should().BeTrue();
		await Task.Delay(50);
		sut.Executions.Should().BeEmpty();
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
		await Task.Delay(50);

		validated.Should().BeFalse();
		sut.Completions.Should().BeEmpty();
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
		var state = new StateImpl<Person>(Context, Option.Some(new Person()));
		var executed = 0;
		var sut = Create(b => b
			.Given(state)
			.Validation(validator)
			.Then(async (person, ct) => Interlocked.Increment(ref executed)));
		await sut.ExecuteAndWait();

		var result = state.Current.Current.Validation.Should().ContainSingle().Subject;
		result.ErrorMessage.Should().Be("Validation_NameRequired");
		result.MemberNames.Should().Equal(nameof(Person.Name));

		await state.UpdateAsync(_ => new Person { Name = "John" }, CT);
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
		var person = new StateImpl<Person>(Context, Option.Some(new Person()));
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
		await WaitFor(() => Validation(state).SequenceEqual(new[] { "on change updated" }));
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

	private sealed class SutCommand
	{
		private readonly List<Exception> _errors;
		private readonly List<object?> _executions;
		private readonly List<ExecutionCompletedEventArgs> _completions = new();

		public SutCommand(IAsyncCommand command, List<Exception> errors, List<object?> executions)
		{
			Command = (AsyncCommand)command;
			_errors = errors;
			_executions = executions;

			Command.ExecutionCompleted += (snd, args) =>
			{
				lock (_completions)
				{
					_completions.Add(args);
				}
			};
		}

		public AsyncCommand Command { get; }

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

		public async Task WaitForCanExecute()
			=> await WaitFor(() => Command.CanExecute(null));

		public async Task ExecuteAndWait()
		{
			await WaitForCanExecute();

			var count = Completions.Count;
			Command.Execute(null);
			await WaitFor(() => Completions.Count > count);
		}
	}

	public sealed class Person
	{
		[Required(ErrorMessage = "Validation_NameRequired")]
		public string? Name { get; init; }
	}

	private sealed class CustomBuilder : ICommandBuilder<string>
	{
		public IConditionalCommandBuilder<string> When(Predicate<string> canExecute) => throw new NotSupportedException();
		public void Then(AsyncAction<string> execute) => throw new NotSupportedException();
		public void Execute(AsyncAction<string> execute) => throw new NotSupportedException();
	}

	private sealed class TestLocalizer : IStringLocalizer, IEnumerable<KeyValuePair<string, string>>
	{
		private readonly ConcurrentDictionary<string, string> _resources = new();

		public void Add(string name, string value)
			=> _resources[name] = value;

		public LocalizedString this[string name]
			=> _resources.TryGetValue(name, out var value)
				? new LocalizedString(name, value)
				: new LocalizedString(name, name, resourceNotFound: true);

		public LocalizedString this[string name, params object[] arguments]
			=> new(name, string.Format(CultureInfo.CurrentCulture, this[name].Value, arguments));

		public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
			=> _resources.Select(kvp => new LocalizedString(kvp.Key, kvp.Value));

		IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
			=> _resources.GetEnumerator();

		global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
			=> _resources.GetEnumerator();
	}
}
