using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A builder of a command user input.
/// </summary>
public interface ICommandBuilder
{
	/// <summary>
	/// Configures the parameter to use for that command.
	/// </summary>
	/// <typeparam name="T">The type of the parameter.</typeparam>
	/// <param name="parameter">The feed to use as parameter.</param>
	/// <returns></returns>
	/// <remarks>
	/// Configuring the parameter directly on the builder allows the command to be CanExecute == false
	/// if parameter is <see cref="Option{T}.Undefined"/> or <see cref="Option{T}.None"/>.
	/// </remarks>
	public ICommandBuilder<T> Given<T>(IFeed<T> parameter);

	/// <summary>
	/// Configures the action to execute.
	/// </summary>
	/// <param name="execute">The action</param>
	public void Then(AsyncAction execute);

	/// <summary>
	/// Configures the action to execute.
	/// </summary>
	/// <param name="execute">The action</param>
	/// <remarks>This is an alias of <see cref="Then"/>.</remarks>
	public void Execute(AsyncAction execute);

	// public void Then(Signal execute);
}
