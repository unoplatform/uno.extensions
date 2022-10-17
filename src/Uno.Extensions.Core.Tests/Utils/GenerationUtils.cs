using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Uno.Extensions.Core.Generators;

namespace Uno.Extensions.Core.Tests.Utils;

public static class GenerationTestHelper
{
	public static ImmutableArray<Diagnostic> GetDiagnostics(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = AppDomain.CurrentDomain.GetAssemblies()
			.Where(assembly => !assembly.IsDynamic)
			.Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
			.Cast<MetadataReference>();
		var compilation = CSharpCompilation.Create(
			assemblyName: "Tests",
			syntaxTrees: new[] { syntaxTree },
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		var generator = new PropertySelectorGenerator();

		var result = CSharpGeneratorDriver
			.Create(generator)
			.RunGenerators(compilation)
			.GetRunResult();

		return result.Diagnostics;
	}
}
