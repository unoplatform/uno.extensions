using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Reactive.Generator;
using Uno.Extensions.Reactive.Generator.Compat;
using Uno.Extensions.Reactive.Generator.Utils;

namespace Uno.Extensions.Core.Generators;

/// <summary>
/// A generator that generates IKeyEquatable implementation.
/// </summary>
[Generator]
public partial class CompatibilityTypesGenerator : ISourceGenerator
{
	/// <inheritdoc />
	public void Initialize(GeneratorInitializationContext context) { }

	/// <inheritdoc />
	public void Execute(GeneratorExecutionContext context)
	{
#if DEBUGGING_GENERATOR
		var process = Process.GetCurrentProcess().ProcessName;
		if (process.IndexOf("VBCSCompiler", StringComparison.OrdinalIgnoreCase) is not -1
			|| process.IndexOf("csc", StringComparison.OrdinalIgnoreCase) is not -1)
		{
			Debugger.Launch();
		}
#endif

		if (context.IsDisabled("UnoExtensionsGeneration_DisableCompatibilityTypesGenerator"))
		{
			return;
		}

		if (GenerationContext.TryGet<CompatibilityTypesGenerationContext>(context, out var error) is { } bindableContext)
		{
			foreach (var generated in new CompatibilityTypesGenerationTool(bindableContext).Generate())
			{
				context.AddSource(PathHelper.SanitizeFileName(generated.fileName), generated.code);
			}
		}
		else
		{
			throw new InvalidOperationException(error);
		}
	}
}

