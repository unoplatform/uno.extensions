using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Uno.Extensions.Core.Generators;
using Uno.Extensions.Generators.Analyzers;

namespace Uno.Extensions.Core.Tests.Utils;

public static class GenerationTestHelper
{
	public static CompilationWithAnalyzers CreateCompilationWithAnalyzers(string source)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(source);
		var references = AppDomain.CurrentDomain.GetAssemblies()
			.Where(assembly => !assembly.IsDynamic)
			.Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
			.Cast<MetadataReference>();
		return CSharpCompilation.Create(
			assemblyName: "Tests",
			syntaxTrees: new[] { syntaxTree },
			references: references,
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
			.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new PropertySelectorAnalyzer()));
	}

	public static (GeneratorRunResult FirstRunResult, GeneratorRunResult SecondRunResult) RunGeneratorTwice(Compilation compilation)
	{
		GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new PropertySelectorGenerator().AsSourceGenerator() }, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

		driver = driver.RunGenerators(compilation);
		var result1 = driver.GetRunResult().Results[0];

		driver = driver.RunGenerators(compilation);
		var result2 = driver.GetRunResult().Results[0];
		return (result1, result2);
	}

	public static void AssertGeneratorResult(GeneratorRunResult result, string? generatedCode, IncrementalStepRunReason expectedReason)
	{
		result.TrackedSteps.Count.Should().Be(4);
		foreach (var trackedStep in result.TrackedSteps.Values)
		{
			foreach (var runStep in trackedStep)
			{
				foreach (var output in runStep.Outputs)
				{
					output.Reason.Should().Be(expectedReason);
				}
			}
		}
		result.Diagnostics.Length.Should().Be(0);
		result.GeneratedSources.Length.Should().Be(generatedCode is null ? 0 : 1);
		if (generatedCode is not null)
		{
			result.GeneratedSources[0].SyntaxTree.ToString().Should().Be(generatedCode);
		}
	}
}
