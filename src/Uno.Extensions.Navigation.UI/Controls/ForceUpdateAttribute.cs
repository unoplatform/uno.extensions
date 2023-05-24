using System;
namespace Uno.Extensions.Navigation.UI.Controls;

/// <summary>
/// Flags the default constructor to use to create an instance of a record that is being de-normalized for bindings.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ForceUpdateAttribute : Attribute
{
	/// <summary>
	/// Gets a value indicating whether the method should be generated or not.
	/// </summary>
	public bool IsEnabled { get; }

	/// <summary>
	/// Configure generation the force update method.
	/// </summary>
	/// <param name="isEnabled">Indicates if the method should be generated or not.</param>
	public ForceUpdateAttribute(bool isEnabled = true)
	{
		IsEnabled = isEnabled;
	}

}
