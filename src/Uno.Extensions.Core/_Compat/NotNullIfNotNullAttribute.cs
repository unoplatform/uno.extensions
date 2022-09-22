using System;
using System.Linq;

#if NETSTANDARD2_0 || WINDOWS_UWP || NET461
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Specifies that the output will be non-null if the named parameter is non-null.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
internal class NotNullIfNotNullAttribute : Attribute
{
	/// <summary>
	/// Gets the associated parameter name.
	/// </summary>
	public string ParameterName { get; }

	/// <summary>
	/// Initializes the attribute with the associated parameter name.
	/// </summary>
	/// <param name="parameterName">The associated parameter name. The output will be non-null if the argument to the parameter specified is non-null.</param>
	public NotNullIfNotNullAttribute(string parameterName)
	{
		ParameterName = parameterName;
	}
}
#endif
