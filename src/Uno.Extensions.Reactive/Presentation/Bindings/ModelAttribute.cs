using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Flags a class as a _model_.
/// </summary>
/// <remarks>This attribute is added by the feeds generator on the _model_ type, you should not have to use it.</remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
[AttributeUsage(AttributeTargets.Class)]
public class ModelAttribute : Attribute
{
	/// <summary>
	/// Flags a class as a _model_.
	/// </summary>
	/// <param name="viewModel">The type of the _view model_.</param>
	public ModelAttribute(Type viewModel)
	{
	}
}
