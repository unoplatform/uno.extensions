using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Generator;

internal static partial class Rules
{
	// references:
	//	Categories: https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/categories

	private static class Category
	{
		// cf. https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/categories

		/// <summary>
		/// Usage rules support proper usage of .NET.
		/// </summary>
		public const string Usage = "Usage";
	}
}
