using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Reactive.Generator.Utils;
using Uno.SourceGeneration;

namespace Uno.Extensions.Reactive.Generator;

[Uno.SourceGeneration.Generator]
public partial class FeedsGenerator : Uno.SourceGeneration.ISourceGenerator
{
	public void Initialize(Uno.SourceGeneration.GeneratorInitializationContext context) { }

	public void Execute(Uno.SourceGeneration.GeneratorExecutionContext context)
	{
		if (BindableGenerationContext.TryGet(context, out var error) is {} bindableContext)
		{
			foreach (var generated in new BindableViewModelGenerator(bindableContext).Generate(context.Compilation.Assembly))
			{
				context.AddSource(PathHelper.SanitizeFileName(generated.type.ToString()), generated.code);
			}
		}
		else
		{
			context.GetLogger().Error(error);
			throw new InvalidOperationException(error);
		}
	}
}
