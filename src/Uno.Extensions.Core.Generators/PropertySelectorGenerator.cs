using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.Extensions.Generators.PropertySelector;

namespace Uno.Extensions.Core.Generators;

/// <summary>
/// A generator that generates IKeyEquatable implementation.
/// </summary>
[Generator]
public partial class PropertySelectorGenerator : IIncrementalGenerator
{
	private readonly PropertySelectorsGenerationTool _tool;

	/// <summary>
	/// Creates a new instance of the PropertySelectorGenerator
	/// </summary>
	public PropertySelectorGenerator()
	{
		_tool = new PropertySelectorsGenerationTool();
	}

	/// <inheritdoc />
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
#if DEBUGGING_GENERATOR
		var process = Process.GetCurrentProcess().ProcessName;
		if (process.IndexOf("VBCSCompiler", StringComparison.OrdinalIgnoreCase) is not -1
			|| process.IndexOf("csc", StringComparison.OrdinalIgnoreCase) is not -1)
		{
			Debugger.Launch();
		}
#endif

		var provider = context.SyntaxProvider
			.CreateSyntaxProvider(
				(node, ct) => node.IsKind(SyntaxKind.InvocationExpression),
				(ctx, ct) => new PropertySelectorCandidate(ctx, ct))
			.Where(candidate => candidate.IsValid);

		// We use the Implementation as the generated code does not alter the SemanticModel (only generates a registry).
		context.RegisterImplementationSourceOutput(provider, _tool.Generate);
	}
}
