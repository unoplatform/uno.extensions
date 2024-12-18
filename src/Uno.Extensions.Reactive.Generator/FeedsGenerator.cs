using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;
using Uno.Extensions.Reactive.Config;

namespace Uno.Extensions.Reactive.Generator;

/// <summary>
/// A generator that generates bindable VM for the reactive framework
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
			var tool = bindableContext.Context.Compilation.Assembly.FindAttribute<BindableGenerationToolAttribute>() ?? new BindableGenerationToolAttribute();
			switch (tool.Version)
			{
				case 1:
					foreach (var generated in new ViewModelGenTool_1(bindableContext).Generate())
					{
						context.AddSource(PathHelper.SanitizeFileName(generated.fileName) + ".g.cs", generated.code);
					}
					break;

				case 2:
					foreach (var generated in new ViewModelGenTool_2(bindableContext).Generate())
					{
						context.AddSource(PathHelper.SanitizeFileName(generated.fileName) + ".g.cs", generated.code);
					}
					break;

				case 3:
					foreach (var (fileName, code) in new ViewModelGenTool_3(bindableContext).Generate())
					{
						context.AddSource(PathHelper.SanitizeFileName(fileName) + ".g.cs", code);
					}
					break;
			}
		}
		else
		{
			throw new InvalidOperationException(error);
		}
	}
}
