using global::System;

#if NETSTANDARD2_0 || WINDOWS_UWP || NET461
namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>
	/// Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the corresponding type allows it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal class NotNullWhenAttribute : Attribute
	{
		/// <summary>
		/// Gets the return value condition.
		/// </summary>
		public bool ReturnValue { get; }

		/// <summary>
		/// The return value condition. If the method returns this value, the associated parameter will not be null.
		/// </summary>
		/// <param name="returnValue"></param>
		public NotNullWhenAttribute(bool returnValue)
		{
			ReturnValue = returnValue;
		}
	}
}
#endif
