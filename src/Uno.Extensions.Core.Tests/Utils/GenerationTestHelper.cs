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

	public static void RunGeneratorTwice(Compilation compilation, Action<GeneratorRunResult> assertFirst, Action<GeneratorRunResult> assertSecond, string? generatedCode)
	{
		var (oldCompilationWeakReference, driver) = RunGeneratorTwiceCore(compilation, assertFirst, assertSecond, generatedCode);
		GC.Collect();
		oldCompilationWeakReference.IsAlive.Should().BeFalse();
		GC.KeepAlive(driver);
	}

	private static (WeakReference CompilationWeakReference, GeneratorDriver Driver) RunGeneratorTwiceCore(Compilation compilation, Action<GeneratorRunResult> assertFirst, Action<GeneratorRunResult> assertSecond, string? generatedCode)
	{
		var dummyTree = SyntaxFactory.ParseSyntaxTree("class DummyClass { }");
		var compilation1 = compilation.AddSyntaxTrees(dummyTree);
		GeneratorDriver driver = CSharpGeneratorDriver.Create(
			new[] { new PropertySelectorGenerator().AsSourceGenerator() },
			driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

		driver = driver.RunGenerators(compilation1);
		var result1 = driver.GetRunResult().Results[0];
		assertFirst(result1);
		AssertNoDiagnosticsAndGeneratedCode(result1, generatedCode);
		var oldCompilationWeakReference = new WeakReference(compilation1);

		var compilation2 = compilation1.ReplaceSyntaxTree(dummyTree, SyntaxFactory.ParseSyntaxTree("class DummyClass2 { }"));
		driver = driver.RunGenerators(compilation2);
		var result2 = driver.GetRunResult().Results[0];
		assertSecond(result2);
		AssertNoDiagnosticsAndGeneratedCode(result2, generatedCode);

		return (oldCompilationWeakReference, driver);
	}

	private static void AssertNoDiagnosticsAndGeneratedCode(GeneratorRunResult result, string? generatedCode)
	{
		result.Diagnostics.Length.Should().Be(0);
		result.GeneratedSources.Length.Should().Be(generatedCode is null ? 0 : 1);
		if (generatedCode is not null)
		{
			result.GeneratedSources[0].SyntaxTree.ToString().Should().Be(generatedCode);
		}
	}

	public static void AssertRunReason(GeneratorRunResult result, IncrementalStepRunReason expectedReason, int expectedTrackedStepsCount = 4)
	{
		result.TrackedSteps.Count.Should().Be(expectedTrackedStepsCount);
		foreach (var trackedStep in result.TrackedSteps.Values)
		{
			foreach (var runStep in trackedStep)
			{
				foreach (var output in runStep.Outputs)
				{
					var adjustedExpectedReason = runStep.Name == "Compilation" && expectedReason == IncrementalStepRunReason.Cached
						? IncrementalStepRunReason.Modified
						: expectedReason;
					output.Reason.Should().Be(adjustedExpectedReason);
				}
			}
		}
	}
}
