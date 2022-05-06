using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Uno.Extensions.Reactive.Generator.Utils;

namespace Uno.Extensions.Reactive.Generator;

[Generator]
public partial class FeedsGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context) { }

	public void Execute(GeneratorExecutionContext context)
	{
#if DEBUG
		var process = Process.GetCurrentProcess().ProcessName;
		if (process.IndexOf("VBCSCompiler", StringComparison.OrdinalIgnoreCase) is not -1
			|| process.IndexOf("csc", StringComparison.OrdinalIgnoreCase) is not -1)
		{
			Debugger.Launch();
		}
#endif

		if (BindableGenerationContext.TryGet(context, out var error) is {} bindableContext)
		{
			foreach (var generated in new BindableViewModelGenerator(bindableContext).Generate(context.Compilation.Assembly))
			{
				context.AddSource(PathHelper.SanitizeFileName(generated.type.ToString()), generated.code);
			}
		}
		else
		{
			//context.GetLogger().Error(error);
			throw new InvalidOperationException(error);
		}
	}
}
