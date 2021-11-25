using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.SourceGeneration;

namespace Uno.Extensions.Reactive.Generator;

[Uno.SourceGeneration.Generator]
public partial class FeedsGenerator : Uno.SourceGeneration.ISourceGenerator
{
	public void Initialize(Uno.SourceGeneration.GeneratorInitializationContext context) { }

	public void Execute(Uno.SourceGeneration.GeneratorExecutionContext context)
	{
		Debugger.Launch();

		if (BindableGenerationContext.TryGet(context, out var error) is {} bindableContext)
		{
			foreach (var generated in new BindableViewModelGenerator(bindableContext).Generate(context.Compilation.Assembly))
			{
				context.AddSource(generated.type.ToString(), generated.code);
			}
		}
		else
		{
			context.GetLogger().Info(error);
		}
	}
}
