using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Commands;

/// <summary>
/// A builder of <see cref="IAsyncCommand"/>.
/// </summary>
/// <typeparam name="T">Expected type of the parameter provided by the view to execute the command.</typeparam>
public readonly struct CommandBuilder<T> : ICommandBuilder, ICommandBuilder<T>, IConditionalCommandBuilder<T>, IValidatingCommandBuilder
{
	private readonly string _name;
	private readonly IList<CommandConfig> _configs;
	private readonly CommandConfig _current;
	private readonly IFeed<T>? _parameter;
	private readonly Func<object?, CancellationToken, ValueTask<bool>>? _validate;

	/// <summary>
	/// Creates a new builder.
	/// </summary>
	/// <param name="name">The name of the command.</param>
	public CommandBuilder(string name)
	{
		_name = name;
		_configs = new List<CommandConfig>();
		_current = default;
	}

	private CommandBuilder(string name, IList<CommandConfig> configs, CommandConfig current, IFeed<T>? parameter, Func<object?, CancellationToken, ValueTask<bool>>? validate)
	{
		_name = name;
		_configs = configs;
		_current = current;
		_parameter = parameter;
		_validate = validate;
	}

	/// <summary>
	/// Builds a command.
	/// </summary>
	/// <param name="context">The source context to use in the command.</param>
	/// <param name="errorHandler">An exception handler.</param>
	/// <returns>The command.</returns>
	public IAsyncCommand Build(SourceContext context, Action<Exception>? errorHandler = null)
		=> new AsyncCommand(_name, _configs, errorHandler ?? Command.DefaultErrorHandler, context);

	ICommandBuilder<TArg> ICommandBuilder.Given<TArg>(IFeed<TArg> parameter)
		=> new CommandBuilder<TArg>(_name, _configs, _current with { Parameter = ctx => ctx.GetOrCreateSource(parameter) }, parameter, validate: null);

	IConditionalCommandBuilder<T> ICommandBuilder<T>.When(Predicate<T> canExecute)
		=> new CommandBuilder<T>(_name, _configs, _current with { CanExecute = arg => canExecute((T)arg!)}, _parameter, _validate);

	object IValidatingCommandBuilder.Validation(Func<object?, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator)
		=> new CommandBuilder<T>(_name, _configs, _current, _parameter, CreateValidation(_name, _parameter, validator));

	void ICommandBuilder.Then(AsyncAction execute)
		=> _configs.Add(_current with { Execute = WithValidation((_, ct) => execute(ct)) });

	void ICommandBuilder.Execute(AsyncAction execute)
		=> _configs.Add(_current with { Execute = WithValidation((_, ct) => execute(ct)) });

	void ICommandBuilder<T>.Then(AsyncAction<T> execute)
		=> _configs.Add(_current with { Execute = WithValidation((arg, ct) => execute((T)arg!, ct)) });

	void ICommandBuilder<T>.Execute(AsyncAction<T> execute)
		=> _configs.Add(_current with { Execute = WithValidation((arg, ct) => execute((T)arg!, ct)) });

	void IConditionalCommandBuilder<T>.Then(AsyncAction<T> execute)
		=> _configs.Add(_current with { Execute = WithValidation((arg, ct) => execute((T)arg!, ct)) });

	/// <summary>
	/// Runs the validation (if any) of the parameter before the action, aborting the execution if the parameter is not valid.
	/// </summary>
	private AsyncAction<object?> WithValidation(AsyncAction<object?> execute)
	{
		if (_validate is not { } validate)
		{
			return execute;
		}

		return async (parameter, ct) =>
		{
			if (!await validate(parameter, ct).ConfigureAwait(false))
			{
				return; // The parameter is not valid, results have been published, abort the execution.
			}

			// The validator might have completed after the command has been disposed (if it does not honor the cancellation token).
			ct.ThrowIfCancellationRequested();

			await execute(parameter, ct).ConfigureAwait(false);
		};
	}

	private static Func<object?, CancellationToken, ValueTask<bool>> CreateValidation(
		string name,
		IFeed<T>? parameter,
		Func<object?, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator)
	{
		// Note: A state created by the State factories is an IValidationTarget no matter the type of the parameter,
		//		 so results can be published even if the parameter is a state of a derived type (covariance).
		Func<IImmutableList<ValidationResult>, CancellationToken, ValueTask>? publish = parameter switch
		{
			IValidationTarget target => target.PublishValidationAsync,
			IState<T> state => (results, ct) => state.UpdateMessageAsync(msg => msg.Validation(results), ct),
			_ => null,
		};

		return async (value, ct) =>
		{
			IImmutableList<ValidationResult> results;
			try
			{
				results = ValidationHelper.ToResults(await validator(value, ct).ConfigureAwait(false)) ?? ImmutableList<ValidationResult>.Empty;
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception error)
			{
				// Wrapped to give context (without the value which might contain user input),
				// but also to make sure that a cancellation which is not ours (e.g. the timeout of an HttpClient) faults the execution instead of silently cancelling it.
				throw new InvalidOperationException($"The validation of the parameter of the command '{name}' failed.", error);
			}

			if (publish is not null)
			{
				// Results are published unconditionally (no matter if the state has changed since the execution started),
				// as they describe the submitted value and are expected to remain until the next execution.
				await publish(results, ct).ConfigureAwait(false);
			}

			if (results.Count is 0)
			{
				return true;
			}

			// Note: We do not log the messages as they might contain user input.
			var log = LogExtensions.Log<AsyncCommand>();
			if (publish is null)
			{
				if (log.IsEnabled(LogLevel.Warning))
				{
					log.LogWarning(
						"The execution of the command '{Command}' has been aborted as its parameter is not valid ({Count} validation errors), "
						+ "but the validation results cannot be published as the parameter of the command is not a state (cf. FEED2003).",
						name,
						results.Count);
				}
			}
			else if (log.IsEnabled(LogLevel.Debug))
			{
				log.LogDebug("The execution of the command '{Command}' has been aborted as its parameter is not valid ({Count} validation errors).", name, results.Count);
			}

			return false;
		};
	}
}
