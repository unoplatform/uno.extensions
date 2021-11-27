using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A builder of a command user input with a parameter.
/// </summary>
/// <typeparam name="T">Type of the parameter.</typeparam>
public interface ICommandBuilder<out T>
{
	/// <summary>
	/// Configures the predicate to apply on parameter to determine the CanExecute state.
	/// </summary>
	/// <param name="canExecute">The predicate.</param>
	/// <returns>The command builder to complete fluent configuration.</returns>
	public IConditionalCommandBuilder<T> When(Predicate<T> canExecute);

	/// <summary>
	/// Configures the action to execute.
	/// </summary>
	/// <param name="execute">The action</param>
	/// <remarks>This is an alias of <see cref="Then"/>.</remarks>
	public void Then(AsyncAction<T> execute);

	/// <summary>
	/// Configures the action to execute.
	/// </summary>
	/// <param name="execute">The action</param>
	/// <remarks>This is an alias of <see cref="Then"/>.</remarks>
	public void Execute(AsyncAction<T> execute);

	//public void Then(Signal execute);
}
