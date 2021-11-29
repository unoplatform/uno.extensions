using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Configure generation of the binding friendly alter-ego of an object.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter)]
public class ReactiveBindableAttribute : Attribute
{
	/// <summary>
	/// Indicates if the object should be generated or not.
	/// </summary>
	public bool IsEnabled { get; }

	/// <summary>
	/// Configure generation of the binding friendly alter-ego of an object.
	/// </summary>
	/// <param name="isEnabled">Indicates if the object should be generated or not.</param>
	public ReactiveBindableAttribute(bool isEnabled = true)
	{
		IsEnabled = isEnabled;
	}
}
