using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
#if !WINUI
using Windows.System;
#else
using Microsoft.UI.Dispatching;
#endif
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A builder of <see cref="IAsyncCommand"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct CommandBuilder<T> : ICommandBuilder, ICommandBuilder<T>, IConditionalCommandBuilder<T>
{
	private readonly string _name;
	private readonly IList<CommandConfig> _configs;
	private readonly CommandConfig _current;

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

	private CommandBuilder(string name, IList<CommandConfig> configs, CommandConfig current)
	{
		_name = name;
		_configs = configs;
		_current = current;
	}

	/// <summary>
	/// Builds a command.
	/// </summary>
	/// <param name="context">The source context to use in the command.</param>
	/// <param name="errorHandler">An exception handler.</param>
	/// <param name="dispatcher">The dispatcher to use to raise <see cref="ICommand.CanExecute"/>.</param>
	/// <returns>The command.</returns>
	public IAsyncCommand Build(SourceContext context, Action<Exception>? errorHandler = null, DispatcherQueue? dispatcher = null)
		=> new AsyncCommand(_name, _configs, errorHandler ?? Command._defaultErrorHandler, context, dispatcher);

	ICommandBuilder<TArg> ICommandBuilder.Given<TArg>(IFeed<TArg> parameter)
		=> new CommandBuilder<TArg>(_name, _configs, _current with { Parameter = ctx => ctx.GetOrCreateSource(parameter) });

	IConditionalCommandBuilder<T> ICommandBuilder<T>.When(Predicate<T> canExecute)
		=> new CommandBuilder<T>(_name, _configs, _current with { CanExecute = arg => canExecute((T)arg!)});

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
}
