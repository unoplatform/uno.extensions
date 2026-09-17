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

	[TestMethod]
	public void When_ModelCarriesNoMetadata_Then_MockIsGeneratedFromSource()
	{
		// The single-project shape: no [Model]/[FeedDependency] to read, because in a real app they are
		// emitted by a sibling generator. The service reaches the feed through the record's synthesized
		// property, and the view-model is named and constructed from the model.
		var (sources, diagnostics) = Run("""
			using Uno.Extensions.Reactive;

			namespace App;

			public interface IPantryService { IListFeed<string> Items { get; } }

			public partial record PantryModel(IPantryService Service)
			{
				public IListFeed<string> Items => Service.Items;
			}
			""");

		diagnostics.Should().BeEmpty();

		var mock = sources.Should().ContainSingle().Subject;
		mock.Should().Contain("public required global::Uno.Extensions.Reactive.IListFeed<string> Items");
		mock.Should().Contain("new global::App.PantryViewModel(default(global::App.IPantryService)! /* Service */)");
	}

	[TestMethod]
	public void When_MockingIsDisabled_Then_SourcePathGeneratesNothing()
	{
		var (sources, diagnostics) = Run("""
			using Uno.Extensions.Reactive;
			using Uno.Extensions.Reactive.Config;

			[assembly: EnableFeedMocking(IsEnabled = false)]

			namespace App;

			public interface IPantryService { IListFeed<string> Items { get; } }

			public partial record PantryModel(IPantryService Service)
			{
				public IListFeed<string> Items => Service.Items;
			}
			""");

		sources.Should().BeEmpty();
		diagnostics.Should().BeEmpty();
	}

	[TestMethod]
	public void When_ModelIsInternal_Then_MockCarriesTheSameAccessibility()
	{
		// The generated view-model is as visible as the model, so a public mock over it would not compile.
		var (sources, _) = Run("""
			using Uno.Extensions.Reactive;

			namespace App;

			public interface IPantryService { IListFeed<string> Items { get; } }

			internal partial record PantryModel(IPantryService Service)
			{
				public IListFeed<string> Items => Service.Items;
			}
			""");

		var mock = sources.Should().ContainSingle().Subject;
		mock.Should().Contain("internal sealed record PantryModelMock");
		mock.Should().Contain("internal static partial class PantryViewModelMock");
	}

	[TestMethod]
	public void When_StateIsServiceDependent_Then_MockExposesItsFeedInterface()
	{
		// FeedMock.Empty hands back an IFeed, so a member typed as the state itself could not accept it.
		var (sources, _) = Run("""
			using Uno.Extensions.Reactive;

			namespace App;

			public interface IPantryService { IState<string> Filter { get; } }

			public partial record PantryModel(IPantryService Service)
			{
				public IState<string> Filter => Service.Filter;
			}
			""");

		sources.Should().ContainSingle()
			.Which.Should().Contain("public required global::Uno.Extensions.Reactive.IFeed<string> Filter");
	}

	[TestMethod]
	public void When_NoFeedIsServiceDependent_Then_ReportsMock0002AndEmitsNothing()
	{
		var (sources, diagnostics) = Run("""
			using Uno.Extensions.Reactive;

			namespace App;

			public partial record PantryModel
			{
				public IFeed<string> Title => null!;
			}
			""");

		sources.Should().BeEmpty();
		diagnostics.Should().ContainSingle().Which.Id.Should().Be("MOCK0002");
	}

	[TestMethod]
	public void When_ViewModelReachedThroughMetadataIsInternal_Then_MockCarriesItsAccessibility()
	{
		// Roslyn exposes the internal types of a metadata reference, so an internal model is enumerated on
		// this path too. A public mock over it would return an inaccessible view-model (CS0122).
		var (sources, diagnostics) = Run(Preamble + """
			[Model(typeof(ItemsViewModel))]
			[FeedDependency("Items", OnParameter = "service")]
			internal class ItemsModel
			{
				public ItemsModel(IService service) { }
				public IListFeed<string> Items => null!;
			}

			internal class ItemsViewModel
			{
				public ItemsViewModel(IService service) { }
				protected ItemsViewModel(ItemsModel model) { }
				public ItemsModel Model => null!;
			}
			""");

		diagnostics.Should().BeEmpty();

		var mock = sources.Should().ContainSingle().Subject;
		mock.Should().Contain("internal sealed record ItemsModelMock");
		mock.Should().Contain("internal static partial class ItemsViewModelMock");
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
