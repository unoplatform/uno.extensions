#if NETSTANDARD2_0 || WINDOWS_UWP || NET461
using System;

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>Specifies that the method or property will ensure that the listed field and property members have non-null values when returning with the specified return value condition.</summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
	internal sealed class MemberNotNullWhenAttribute : Attribute
	{
		/// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
		/// <param name="returnValue">The return value condition. If the method returns this value, the associated parameter will not be <see langword="null" />.</param>
		/// <param name="member">The field or property member that is promised to be non-null.</param>
		public MemberNotNullWhenAttribute(bool returnValue, string member)
		{
			ReturnValue = returnValue;
			Members = new string[1] { member };
		}

		/// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
		/// <param name="returnValue">The return value condition. If the method returns this value, the associated parameter will not be <see langword="null" />.</param>
		/// <param name="members">The list of field and property members that are promised to be non-null.</param>
		public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
		{
			ReturnValue = returnValue;
			Members = members;
		}

		/// <summary>Gets the return value condition.</summary>
		public bool ReturnValue { get; }

		/// <summary>Gets field or property member names.</summary>
		public string[] Members { get; }
	}
}
#endif  // NETSTANDARD2_0 || WINDOWS_UWP || NET461
