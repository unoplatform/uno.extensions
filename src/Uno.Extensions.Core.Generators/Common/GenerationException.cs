using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Extensions.Generators;

internal class GenerationException : Exception
{
	public Diagnostic[] Diagnostics { get; }

	public GenerationException(params Diagnostic[] diagnostics)
		: base(diagnostics.Select(diag => diag?.Descriptor?.Title?.ToString()).JoinBy(". | ") + ".")
	{
		Diagnostics = diagnostics;
	}
}
