using System;
using System.Linq;
using System.Windows.Input;

namespace Uno.Extensions.Reactive.Commands;

/// <summary>
/// Defines if the a method should be exposed to binding through an <see cref="ICommand"/> or not.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
	/// <summary>
	/// Defines if the a method should be exposed to binding through an <see cref="ICommand"/> or not.
	/// </summary>
	/// <param name="isEnabled"></param>
	public CommandAttribute(bool isEnabled = true)
	{
	}
}
