using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.HotTesting.Reactive.Generator;

namespace Uno.HotTesting.Reactive.Tests;

/// <summary>
/// Drives the consumer generator over hand-written metadata (the attributes and view-model the MVUX
/// generator would have produced) to pin the emitted <c>Create</c> and the diagnostics. The two-project
/// fixture app cannot host these cases: a warning fails its build and an ambiguous call never compiles.
/// </summary>
[TestClass]
public class Given_FeedsMockGenerator
{
	private const string Preamble = """
		using Uno.Extensions.Reactive;
		using Uno.Extensions.Reactive.Bindings;
		using Uno.Extensions.Reactive.Config;

		namespace App;

		public interface IService { }
		public interface ISnapshot { }

		""";

	[TestMethod]
	public void When_ViewModelHasPublicCtor_Then_CreateNullInjectsTypedDefaults()
	{
		var (sources, diagnostics) = Run(Preamble + """
			[Model(typeof(ItemsViewModel))]
			[FeedDependency("Items", OnParameter = "service")]
			public class ItemsModel
			{
				public ItemsModel(IService service) { }
				public IListFeed<string> Items => null!;
			}

			public class ItemsViewModel
			{
				public ItemsViewModel(IService service) { }
				protected ItemsViewModel(ItemsModel model) { }
				public ItemsModel Model => null!;
			}
			""");

		diagnostics.Should().BeEmpty();
		sources.Should().ContainSingle()
			.Which.Should().Contain("new global::App.ItemsViewModel(default(global::App.IService)! /* service */)");
	}

	[TestMethod]
	public void When_ViewModelHasSameArityCtors_Then_CreateTargetsOrdinalFirstParameterTypes()
	{
		// Two single-parameter constructors: the typed default makes the call unambiguous, and the ordinal
		// tie-break on the parameter type list keeps the output stable whatever order the symbols come in.
		var (sources, diagnostics) = Run(Preamble + """
			[Model(typeof(ItemsViewModel))]
			[FeedDependency("Items", OnParameter = "service")]
			public class ItemsModel
			{
				public ItemsModel(IService service) { }
				public ItemsModel(ISnapshot snapshot) { }
				public IListFeed<string> Items => null!;
			}

			public class ItemsViewModel
			{
				public ItemsViewModel(ISnapshot snapshot) { }
				public ItemsViewModel(IService service) { }
				protected ItemsViewModel(ItemsModel model) { }
				public ItemsModel Model => null!;
			}
			""");

		diagnostics.Should().BeEmpty();
		sources.Should().ContainSingle()
			.Which.Should().Contain("new global::App.ItemsViewModel(default(global::App.IService)! /* service */)");
	}

	[TestMethod]
	public void When_ViewModelHasNoPublicCtor_Then_ReportsMOCK0001AndEmitsNothing()
	{
		var (sources, diagnostics) = Run(Preamble + """
			[Model(typeof(ItemsViewModel))]
			[FeedDependency("Items", OnParameter = "service")]
			public class ItemsModel
			{
				internal ItemsModel(IService service) { }
				public IListFeed<string> Items => null!;
			}

			public class ItemsViewModel
			{
				internal ItemsViewModel(IService service) { }
				protected ItemsViewModel(ItemsModel model) { }
				public ItemsModel Model => null!;
			}
			""");

		sources.Should().BeEmpty();
		var diagnostic = diagnostics.Should().ContainSingle().Which;
		diagnostic.Id.Should().Be("MOCK0001");
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
		diagnostic.GetMessage().Should().Contain("ItemsModel").And.Contain("ItemsViewModel");
	}

	private static (string[] Sources, Diagnostic[] Diagnostics) Run(string source)
	{
		// The framework plus the Uno.Extensions assemblies of the test host: enough for the fixture source to
		// compile, small enough for the generator's walk over referenced assemblies to stay quick.
		var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
			.Split(Path.PathSeparator)
			.Where(path => Path.GetFileName(path) is { } name
				&& (name.StartsWith("System.", StringComparison.Ordinal)
					|| name.StartsWith("netstandard", StringComparison.Ordinal)
					|| name.StartsWith("Uno.Extensions.", StringComparison.Ordinal)))
			.Select(path => MetadataReference.CreateFromFile(path));
		var compilation = CSharpCompilation.Create(
			"App",
			new[] { CSharpSyntaxTree.ParseText(source) },
			references,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
		compilation.GetDiagnostics()
			.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
			.Should().BeEmpty("the fixture source must compile before the generator runs");

		var result = CSharpGeneratorDriver.Create(new FeedsMockGenerator())
			.RunGenerators(compilation)
			.GetRunResult()
			.Results
			.Single();

		return (result.GeneratedSources.Select(generated => generated.SourceText.ToString()).ToArray(), result.Diagnostics.ToArray());
	}
}
