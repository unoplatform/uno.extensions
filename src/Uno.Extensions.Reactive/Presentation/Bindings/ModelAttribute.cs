using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Flags a class as a _model_.
/// </summary>
/// <remarks>This attribute is added by the feeds generator on the _model_ type, you should not have to use it.</remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ModelAttribute : Attribute
{
	internal const DynamicallyAccessedMemberTypes BindableRequirements =
		  DynamicallyAccessedMemberTypes.PublicConstructors
		| DynamicallyAccessedMemberTypes.PublicProperties
		;

	/// <summary>
	/// Type of the generated bindable for the _model_.
	/// </summary>
	[DynamicallyAccessedMembers(BindableRequirements)]
	public Type Bindable { get; }

	/// <summary>
	/// Flags a class as a _model_.
	/// </summary>
	/// <param name="bindable">The type of the _bindable view model_.</param>
	public ModelAttribute(
		[DynamicallyAccessedMembers(BindableRequirements)]
		Type bindable)
	{
		Bindable = bindable;
	}
}
