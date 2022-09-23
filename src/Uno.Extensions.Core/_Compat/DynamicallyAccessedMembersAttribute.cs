#pragma warning disable CS0436 // Attribute is imported in all projects, so there is conflict on DynamicallyAccessedMemberTypes
using System;
using System.Linq;

#if NETSTANDARD2_0 || WINDOWS_UWP || NET461
namespace System.Diagnostics.CodeAnalysis;

/// <summary>Indicates that certain members on a specified <see cref="T:System.Type" /> are accessed dynamically, for example, through <see cref="N:System.Reflection" />.</summary>
[AttributeUsage(AttributeTargets.Class
	| AttributeTargets.Struct
	| AttributeTargets.Method
	| AttributeTargets.Property
	| AttributeTargets.Field
	| AttributeTargets.Interface
	| AttributeTargets.Parameter
	| AttributeTargets.ReturnValue
	| AttributeTargets.GenericParameter, Inherited = false)]
internal sealed class DynamicallyAccessedMembersAttribute : Attribute
{
	/// <summary>Initializes a new instance of the <see cref="T:System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute" /> class with the specified member types.</summary>
	/// <param name="memberTypes">The types of the dynamically accessed members.</param>
	public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
		=> MemberTypes = memberTypes;

	/// <summary>Gets the <see cref="T:System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes" /> that specifies the type of dynamically accessed members.</summary>
	public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

#endif
