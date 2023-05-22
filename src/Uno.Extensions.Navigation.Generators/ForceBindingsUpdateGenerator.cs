using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Navigation.Generators;

/// <summary>
/// A generator that generates bindable VM for the reactive framework
/// </summary>
[Generator]
public partial class ForceBindingsUpdateGenerator : ISourceGenerator
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

		if (GenerationContext.TryGet<ForceBindingsUpdateGenerationContext>(context, out var error) is { } bindableContext)
		{
			foreach (var generated in new ForceBindingsUpdateGenTool_1(bindableContext).Generate())
			{
				context.AddSource(PathHelper.SanitizeFileName(generated.fileName) + ".g.cs", generated.code);
			}
		}
		else
		{
			throw new InvalidOperationException(error);
		}

	}
}
