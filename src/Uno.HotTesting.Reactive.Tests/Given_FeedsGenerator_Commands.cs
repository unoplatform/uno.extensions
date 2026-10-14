using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Generator;

namespace Uno.HotTesting.Reactive.Tests;

/// <summary>
/// Drives the MVUX generator in-process to pin the text of the emitted command <c>Execute</c> lambdas.
/// Lives here because this project already hosts the in-process generator harness; Uno.Extensions.Reactive.Tests
/// only consumes the generator as an analyzer.
/// </summary>
[TestClass]
public class Given_FeedsGenerator_Commands
{
	[TestMethod]
	public void When_MethodIsAwaitable_Then_ExecuteCastsTheCallToTheReturnTypeBeforeAwaiting()
	{
		// The updated-model (Hot Reload) path calls the method on a `dynamic` model. Awaiting that dynamic
		// value crashes Roslyn EnC (dotnet/roslyn#86045), so the call is cast to its declared type first.
		var (sources, diagnostics) = Run("""
			using System.Threading;
			using System.Threading.Tasks;

			namespace App;

			public partial record CounterModel
			{
				public async ValueTask Increment(CancellationToken ct) { }
				public async ValueTask Add(int step, CancellationToken ct) { }
				public async Task Reset(CancellationToken ct) { }
				public ValueTask<int> Load(CancellationToken ct) => default;
				public Task<string?> Name(CancellationToken ct) => Task.FromResult<string?>(null);
				public void Sync() { }
			}
			""");

		diagnostics.Should().BeEmpty();
		var vm = sources.Should().ContainSingle(source => source.Contains("class CounterViewModel")).Subject;

		// Once in the constructor (static model) and once in __Reactive_BindableInitializeForUpdatedModel (dynamic model).
		Occurrences(vm, "await ((global::System.Threading.Tasks.ValueTask)model.Increment(reactive_ct));").Should().Be(2);
		Occurrences(vm, "await ((global::System.Threading.Tasks.ValueTask)model.Add(step, ct));").Should().Be(2);
		Occurrences(vm, "await ((global::System.Threading.Tasks.Task)model.Reset(reactive_ct));").Should().Be(2);
		Occurrences(vm, "await ((global::System.Threading.Tasks.ValueTask<int>)model.Load(reactive_ct));").Should().Be(2);
		Occurrences(vm, "await ((global::System.Threading.Tasks.Task<string>)model.Name(reactive_ct));").Should().Be(2);
		Occurrences(vm, "model.Sync();").Should().Be(2);
		vm.Should().NotContain("await model.");
	}

	private static int Occurrences(string text, string value)
	{
		var count = 0;
		for (var index = text.IndexOf(value, StringComparison.Ordinal); index >= 0; index = text.IndexOf(value, index + value.Length, StringComparison.Ordinal))
		{
			count++;
		}
		return count;
	}

	private static (string[] Sources, Diagnostic[] Diagnostics) Run(string source)
	{
		// Framework plus Uno.Extensions runtime assemblies; the generator assemblies are excluded as they
		// duplicate the public config attributes and would make their lookup by metadata name ambiguous.
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Where(path => Path.GetFileNameWithoutExtension(path) is { } name
				&& !name.EndsWith(".Generator", StringComparison.Ordinal)
				&& (name.StartsWith("System", StringComparison.Ordinal)
					|| name.StartsWith("netstandard", StringComparison.Ordinal)
					|| name.StartsWith("Microsoft.CSharp", StringComparison.Ordinal)
					|| name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal)
					|| name.StartsWith("Uno.Extensions.", StringComparison.Ordinal)
					|| name.StartsWith("Uno.Toolkit", StringComparison.Ordinal)))
			.Select(path => MetadataReference.CreateFromFile(path));
		var compilation = CSharpCompilation.Create(
			"App",
			new[] { CSharpSyntaxTree.ParseText(source) },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

		var driver = CSharpGeneratorDriver.Create(new FeedsGenerator())
			.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
		var result = driver.GetRunResult().Results.Single();

		// The generated code must compile too: the cast has to be valid on the static (constructor) path.
		output.GetDiagnostics()
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.Should().BeEmpty();

		return (result.GeneratedSources.Select(generated => generated.SourceText.ToString()).ToArray(), result.Diagnostics.ToArray());
	}
}
