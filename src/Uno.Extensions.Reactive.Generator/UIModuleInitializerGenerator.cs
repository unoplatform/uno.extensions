using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Generator.Dispatching;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A generator that generates UI module initialization for the reactive framework.
/// </summary>
[Generator]
public class UIModuleInitializerGenerator : ISourceGenerator
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

		if (GenerationContext.TryGet<UIModuleInitializerGenerationContext>(context, out _) is { } dispatcherContext)
		{
			foreach (var generated in new UIModuleInitializerGenerationTool(dispatcherContext).Generate())
			{
				context.AddSource(PathHelper.SanitizeFileName(generated.Name) + ".g.cs", generated.Code);
			}
		}
	}
}
