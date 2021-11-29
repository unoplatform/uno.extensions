using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A builder of a command user input with a parameter and a predicate.
/// </summary>
/// <typeparam name="T">Type of the parameter.</typeparam>
public interface IConditionalCommandBuilder<out T>
{
	/// <summary>
	/// Configures the action to execute.
	/// </summary>
	/// <param name="execute">The action</param>
	/// <remarks>This is an alias of <see cref="Then"/>.</remarks>
	public void Then(AsyncAction<T> execute);

	// public void Then(Signal execute);
}
