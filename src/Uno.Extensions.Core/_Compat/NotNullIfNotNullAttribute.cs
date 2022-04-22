using System;
using System.Linq;

#if NETSTANDARD2_0 || WINDOWS_UWP || NET461
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
internal class NotNullIfNotNullAttribute : Attribute
{
	public string ParameterName { get; }

	public NotNullIfNotNullAttribute(string parameterName)
	{
		ParameterName = parameterName;
	}
}
#endif
