extern alias ReactiveGenerator;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommandValidationAnalyzer = ReactiveGenerator::Uno.Extensions.Reactive.Generator.Commands.CommandValidationAnalyzer;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public class Given_CommandValidationDiagnostic
{
	private const string Header = """
		using System;
		using System.Threading.Tasks;
		using Uno.Extensions.Reactive;

		namespace App;

		public partial class MyModel
		{
			public IState<string> Name => State.Value(this, () => "");
			public IFeed<string> Feed => Uno.Extensions.Reactive.Feed.Async(async ct => "");

		""";

	private const string Footer = """

		}
		""";

	[TestMethod]
	public async Task When_GivenState_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit => Command.Create(b => b
				.Given(Name)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_GivenStateThenWhen_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit => Command.Create(b => b
				.Given(Name)
				.When(name => name is not null)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_GivenStateCastToFeed_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit => Command.Create(b => b
				.Given((IFeed<string>)Name)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().BeEmpty();
	}

	[TestMethod]
	public async Task When_GivenFeed_Then_Diagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit => Command.Create(b => b
				.Given(Feed)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		var diagnostic = diagnostics.Should().ContainSingle().Subject;
		diagnostic.Id.Should().Be("FEED2003");
		diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
		diagnostic.GetMessage().Should().Contain("'Feed'").And.Contain("IFeed<string>");
		diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan).Should().StartWith("Validation(");
	}

	[TestMethod]
	public async Task When_GivenFeedThenWhen_Then_Diagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit => Command.Create(b => b
				.Given(Feed)
				.When(name => name is not null)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().ContainSingle(d => d.Id == "FEED2003");
	}

	[TestMethod]
	public async Task When_GivenCombinedFeed_Then_Diagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit => Command.Create(b => b
				.Given(Uno.Extensions.Reactive.Feed.Combine(Name, Feed))
				.Validation(async (values, ct) => values.Item1.Length > 0, "required")
				.Then(async (values, ct) => { }));
			""");

		diagnostics.Should().ContainSingle(d => d.Id == "FEED2003");
	}

	[TestMethod]
	public async Task When_ParameterFromView_Then_Diagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit => Command.Create<string>(b => b
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().ContainSingle(d => d.Id == "FEED2003")
			.Which.GetMessage().Should().Contain("provided by the view");
	}

	[TestMethod]
	public async Task When_BuilderStoredInLocal_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit => Command.Create(b =>
			{
				var given = b.Given(Feed);
				given.Validation(async (name, ct) => name.Length > 0, "required").Then(async (name, ct) => { });
			});
			""");

		diagnostics.Should().BeEmpty();
	}

	private static async Task<ImmutableArray<Diagnostic>> Run(string members)
	{
		var compilation = GeneratorTestHelper.CreateCompilation(Header + members + Footer);

		return await compilation
			.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new CommandValidationAnalyzer()))
			.GetAnalyzerDiagnosticsAsync();
	}
}
