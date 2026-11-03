using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Uno.Extensions.Reactive.Tests.Generator;

/// <summary>
/// Helpers to run the Reactive generator and analyzers in-process on a fixture source.
/// </summary>
internal static class GeneratorTestHelper
{
	/// <summary>
	/// Creates a compilation of the given source, referencing the BCL, Microsoft.Extensions and Uno.Extensions (but not the generators) assemblies of the test host.
	/// </summary>
	/// <remarks>The fixture source must compile: this asserts that the compilation has no error.</remarks>
	public static CSharpCompilation CreateCompilation(string source)
	{
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Where(path => Path.GetFileName(path) is { } name
				&& (name.StartsWith("System.", StringComparison.Ordinal)
					|| name.StartsWith("netstandard", StringComparison.Ordinal)
					|| name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal)
					|| (name.StartsWith("Uno.Extensions.", StringComparison.Ordinal) && !name.Contains("Generator", StringComparison.Ordinal))))
			.Select(path => MetadataReference.CreateFromFile(path));
		var compilation = CSharpCompilation.Create(
			"App",
			new[] { CSharpSyntaxTree.ParseText(source) },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

		compilation.GetDiagnostics()
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.Should().BeEmpty("the fixture source must compile before the generator / analyzer runs");

		return compilation;
	}
}
