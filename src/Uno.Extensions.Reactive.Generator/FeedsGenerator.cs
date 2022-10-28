using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A generator that generates bindable VM for the feed framework
/// </summary>
[Generator]
public partial class FeedsGenerator : ISourceGenerator
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

		if (GenerationContext.TryGet<BindableGenerationContext>(context, out var error) is {} bindableContext)
		{
			foreach (var generated in new BindableViewModelGenerator(bindableContext).Generate())
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
