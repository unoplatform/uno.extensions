using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Uno.Extensions.Reactive.UI.Config;

/// <summary>
/// Defines for generated commands if parameters should be automatically full-filled based on public feeds properties exposed by the class.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public class ImplicitFeedCommandParametersAttribute : Attribute
{
	internal const bool DefaultValue = true;

	/// <summary>
	/// Defines for generated commands if parameters should be automatically full-filled based on public feeds properties exposed by the class.
	/// </summary>
	/// <param name="isEnabled">True if all compatible parameters of methods exposed as <see cref="ICommand"/> should be full-filled by default on the whole project, false else.</param>
	public ImplicitFeedCommandParametersAttribute(bool isEnabled = DefaultValue)
	{
	}
}
