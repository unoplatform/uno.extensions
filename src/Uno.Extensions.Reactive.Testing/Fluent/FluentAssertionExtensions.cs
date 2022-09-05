using System;
using System.Linq;
using FluentAssertions.Execution;

namespace FluentAssertions;

internal static class FluentAssertionExtensions
{
	public static AssertionScope ForContext(this AssertionScope scope, string additionalContext) 
		=> scope.Context is null
			? new (additionalContext.Trim())
			: new($"{scope.Context.Value} {additionalContext.Trim()}");

	public static void Fail(this AssertionScope scope, string reason)
		=> scope.FailWith($"{scope.Context.Value} {reason.Trim()}");
}
