using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Flags a class as a _view model_ (a.k.a. Bindable).
/// </summary>
/// <remarks>This attribute is added by the feeds generator on the _view model_ type, you should not have to use it.</remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
[AttributeUsage(AttributeTargets.Class)]
public class ViewModelAttribute : Attribute
{
	/// <summary>
	/// Flags a class as a _view model_ (a.k.a. Bindable).
	/// </summary>
	/// <param name="model">The type of the _model_.</param>
	public ViewModelAttribute(Type model)
	{
	}
}
