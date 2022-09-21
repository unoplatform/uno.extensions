using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Reactive.Generator;

[SuppressMessage("ReSharper", "InconsistentNaming")]
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
