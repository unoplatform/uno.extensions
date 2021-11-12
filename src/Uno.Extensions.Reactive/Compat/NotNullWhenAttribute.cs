using System;

#if !NET50
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public class NotNullWhenAttribute : Attribute
{
	public bool ReturnValue { get; }

	public NotNullWhenAttribute(bool returnValue)
	{
		ReturnValue = returnValue;
	}
}
#endif
