#if !NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.CompilerServices
{
	[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public class CallerArgumentExpressionAttribute : Attribute
	{
		public string ParameterName { get; set; }

		public CallerArgumentExpressionAttribute(string parameterName)
		{
			ParameterName = parameterName;
		}
	}

}
#endif