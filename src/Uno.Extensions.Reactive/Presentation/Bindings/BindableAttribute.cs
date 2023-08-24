using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Flags a class as a _bindable_ (a.k.a. _view model_).
/// </summary>
/// <remarks>This attribute is added by the feeds generator on the _view model_ type, you should not have to use it.</remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
[AttributeUsage(AttributeTargets.Class)]
public class BindableAttribute : Attribute
{
	/// <summary>
	/// Type of the _model_ for this _bindable_ (a.k.a. _view model_).
	/// </summary>
	public Type Model { get; }

	/// <summary>
	/// Flags a class as a _view model_ (a.k.a. Bindable).
	/// </summary>
	/// <param name="model">The type of the _model_.</param>
	public BindableAttribute(Type model)
	{
		Model = model;
	}
}

/// <summary>
/// Deprecated attribute used for backward compatibility and migration purposes.
/// </summary>
[Obsolete("Use BindableAttribute instead")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
[AttributeUsage(AttributeTargets.Class)]
public class ViewModelAttribute : BindableAttribute
{
	/// <inheritdoc />
	public ViewModelAttribute(Type model)
		: base(model)
	{
	}
}
