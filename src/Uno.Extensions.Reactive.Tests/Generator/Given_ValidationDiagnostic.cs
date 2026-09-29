extern alias ReactiveGenerator;

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FeedsGenerator = ReactiveGenerator::Uno.Extensions.Reactive.Generator.FeedsGenerator;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public class Given_ValidationDiagnostic
{
	private const string Header = """
		using System;
		using Uno.Extensions.Reactive;

		[assembly: Uno.Extensions.Reactive.Config.BindableGenerationTool(3)]

		namespace App;
		""";

	[TestMethod]
	public void When_ModelDeclaresHasErrors_Then_Diagnostic()
	{
		var (diagnostics, compilation) = Run(Header + """

			public partial class MyModel
			{
				public IState<string> Name => State.Value(this, () => "");

				public IFeed<bool> HasErrors => Feed.Async(async ct => false);
			}
			""");

		diagnostics.Should().ContainSingle(d => d.Id == "FEED1001")
			.Which.GetMessage().Should().Contain("HasErrors").And.Contain("MyModel");
		compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
	}

	[TestMethod]
	public void When_RecordDeclaresHasErrors_Then_Diagnostic()
	{
		var (diagnostics, compilation) = Run(Header + """

			public partial record MyForm(string Name, bool HasErrors);

			public partial class MyModel
			{
				public IState<MyForm> Form => State.Value(this, () => new MyForm("", false));
			}
			""");

		diagnostics.Should().ContainSingle(d => d.Id == "FEED1001")
			.Which.GetMessage().Should().Contain("HasErrors").And.Contain("MyForm");
		compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
	}

	[TestMethod]
	public void When_NoCollision_Then_NoDiagnostic()
	{
		var (diagnostics, compilation) = Run(Header + """

			public partial record MyForm(string Name);

			public partial class MyModel
			{
				public IState<MyForm> Form => State.Value(this, () => new MyForm(""));
			}
			""");

		diagnostics.Should().NotContain(d => d.Id == "FEED1001");
		compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
	}

	private static (Diagnostic[] Diagnostics, Compilation Output) Run(string source)
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
			.Should().BeEmpty("the fixture source must compile before the generator runs");

		CSharpGeneratorDriver
			.Create(new FeedsGenerator())
			.RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);

		return (diagnostics.ToArray(), output);
	}
}
