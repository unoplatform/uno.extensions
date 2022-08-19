using System;
using System.Linq;
using System.Windows.Input;
using Uno.Extensions.Reactive.UI.Presentation.Commands;

namespace Uno.Extensions.Reactive.UI.Config;

/// <summary>
/// Indicates if public methods should be automatically exposed to bindings as <see cref="ICommand"/>.
/// </summary>
/// <remarks>If disabled, you can still generates commands by flagging methods with the <see cref="CommandAttribute"/>.</remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public class ImplicitCommandsAttribute : Attribute
{
	internal const bool DefaultValue = true;

	/// <summary>
	/// Indicates if public methods should be automatically exposed to bindings as <see cref="ICommand"/>.
	/// </summary>
	/// <param name="isEnabled">True if all compatible public methods should be exposed as <see cref="ICommand"/> by default on the whole project, false else.</param>
	public ImplicitCommandsAttribute(bool isEnabled = DefaultValue)
	{
	}
}
