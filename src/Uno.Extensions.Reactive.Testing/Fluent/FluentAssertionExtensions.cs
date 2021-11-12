using System;
using System.Linq;
using FluentAssertions.Execution;

namespace FluentAssertions;

internal static class FluentAssertionExtensions
{
	public static AssertionScope ForContext(this AssertionScope scope, string additionalContext) 
		=> new($"{scope.Context.Value} {additionalContext.Trim()}");
}
