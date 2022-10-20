using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;
using Uno.Extensions.Generators.KeyEquality;

namespace Uno.Extensions.Core.Generators;

/// <summary>
/// A generator that generates IKeyEquatable implementation.
/// </summary>
[Generator]
public partial class KeyEqualityGenerator : ISourceGenerator
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
		if (context.IsDisabled("UnoExtensionsGeneration_DisableKeyEqualityGenerator"))
		{
			return;
		}

		if (GenerationContext.TryGet<KeyEqualityGenerationContext>(context, out var error) is { } bindableContext)
		{
			foreach (var generated in new KeyEqualityGenerationTool(bindableContext).Generate())
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

