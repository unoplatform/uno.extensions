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

	private CommandBuilder(string name, IList<CommandConfig> configs, CommandConfig current, IFeed<T>? parameter)
	{
		_name = name;
		_configs = configs;
		_current = current;
		_parameter = parameter;
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
		=> new CommandBuilder<TArg>(_name, _configs, _current with { Parameter = ctx => ctx.GetOrCreateSource(parameter) }, parameter);

	IConditionalCommandBuilder<T> ICommandBuilder<T>.When(Predicate<T> canExecute)
		=> new CommandBuilder<T>(_name, _configs, _current with { CanExecute = arg => canExecute((T)arg!)}, _parameter);

	object IValidatingCommandBuilder.Validation(Func<object?, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator)
		=> new CommandBuilder<T>(_name, _configs, _current with { Validate = CreateValidation(_name, _parameter as IState<T>, validator) }, _parameter);

	void ICommandBuilder.Then(AsyncAction execute)
		=> _configs.Add(_current with { Execute = (_, ct) => execute(ct) });

	void ICommandBuilder.Execute(AsyncAction execute)
		=> _configs.Add(_current with { Execute = (_, ct) => execute(ct) });

	void ICommandBuilder<T>.Then(AsyncAction<T> execute)
		=> _configs.Add(_current with { Execute = (arg, ct) => execute((T)arg!, ct) });

	void ICommandBuilder<T>.Execute(AsyncAction<T> execute)
		=> _configs.Add(_current with { Execute = (arg, ct) => execute((T)arg!, ct) });

	void IConditionalCommandBuilder<T>.Then(AsyncAction<T> execute)
		=> _configs.Add(_current with { Execute = (arg, ct) => execute((T)arg!, ct) });

	private static Func<object?, CancellationToken, ValueTask<bool>> CreateValidation(
		string name,
		IState<T>? state,
		Func<object?, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator)
		=> async (parameter, ct) =>
		{
			// Note: ValidationResult.Success is null, so we ignore null results.
			var results = (await validator(parameter, ct).ConfigureAwait(false))?.OfType<ValidationResult>().ToImmutableList()
				?? ImmutableList<ValidationResult>.Empty;

			if (state is not null)
			{
				// Results are published unconditionally (no matter if the state has changed since the execution started),
				// as they describe the submitted value and are expected to remain until the next execution.
				await state.UpdateMessageAsync(msg => msg.Validation(results), ct).ConfigureAwait(false);
			}
			else if (results.Count > 0 && LogExtensions.Log<AsyncCommand>() is { } log && log.IsEnabled(LogLevel.Warning))
			{
				// Note: We do not log the messages as they might contain user input.
				log.LogWarning(
					"The execution of the command '{Command}' has been aborted as its parameter is not valid ({Count} validation errors), "
					+ "but the validation results cannot be published as the parameter of the command is not a state (cf. FEED2003).",
					name,
					results.Count);
			}

			return results.Count is 0;
		};
}

/// <summary>
/// A command builder that supports a validation step (cf. CommandBuilderExtensions.Validation).
/// </summary>
internal interface IValidatingCommandBuilder
{
	/// <summary>
	/// Adds a validation of the parameter, run on each execution of the command.
	/// </summary>
	/// <returns>The <see cref="IConditionalCommandBuilder{T}"/> to complete the configuration of the command.</returns>
	object Validation(Func<object?, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator);
}
