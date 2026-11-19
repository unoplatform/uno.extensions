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
	public async Task When_GivenStateDeclaredAsFeed_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			public IFeed<string> NameAsFeed => State.Value(this, () => "");
			public IFeed<string> NameAsFeedWithGetter { get => State.Value(this, () => ""); }
			public IFeed<string> NameAlias => NameAsFeed;

			public IAsyncCommand Submit => Command.Create(b => b
				.Given(NameAsFeed)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));

			public IAsyncCommand Submit2 => Command.Create(b => b
				.Given(NameAsFeedWithGetter)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));

			public IAsyncCommand Submit3 => Command.Create(b => b
				.Given(NameAlias)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().BeEmpty("a feed property which returns a state is a state at runtime");
	}

	[TestMethod]
	public async Task When_GivenFeedOfUnknownImplementation_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			public IFeed<string> External { get { return Uno.Extensions.Reactive.Feed.Async(async ct => ""); } }

			public IAsyncCommand Submit => Command.Create(b => b
				.Given(External)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().BeEmpty("the runtime type of the parameter cannot be determined");
	}

	[TestMethod]
	public async Task When_GivenParameter_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			public static IAsyncCommand SubmitFor<T>(IFeed<T> form)
				=> Command.Create(b => b
					.Given(form)
					.Validation(async (value, ct) => value is not null, "required")
					.Then(async (value, ct) => { }));
			""");

		diagnostics.Should().BeEmpty("the runtime type of the parameter cannot be determined");
	}

	[TestMethod]
	public async Task When_GivenMethodCall_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			private IFeed<string> GetForm() => Name;

			public IAsyncCommand Submit => Command.Create(b => b
				.Given(GetForm())
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().BeEmpty("the runtime type of the parameter cannot be determined");
	}

	[TestMethod]
	public async Task When_GivenConditional_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			public bool UseFeed { get; set; }

			public IAsyncCommand Submit => Command.Create(b => b
				.Given(UseFeed ? Feed : Name)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().BeEmpty("the runtime type of the parameter cannot be determined");
	}

	[TestMethod]
	public async Task When_GivenReadonlyFieldOfUnknownValue_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			private static IFeed<string> Create() => State.Value(new object(), () => "");
			private readonly IFeed<string> _form = Create();

			public IAsyncCommand Submit => Command.Create(b => b
				.Given(_form)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().BeEmpty("the runtime type of the parameter cannot be determined");
	}

	[TestMethod]
	public async Task When_GivenReadonlyFieldOfFeed_Then_Diagnostic()
	{
		var diagnostics = await Run("""
			private readonly IFeed<string> _form = Uno.Extensions.Reactive.Feed.Async(async ct => "");

			public IAsyncCommand Submit => Command.Create(b => b
				.Given(_form)
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().ContainSingle(d => d.Id == "FEED2003");
	}

	[TestMethod]
	public async Task When_GivenLocalOfUnknownValue_Then_NoDiagnostic()
	{
		var diagnostics = await Run("""
			private IFeed<string> GetForm() => Name;

			public IAsyncCommand Submit
			{
				get
				{
					var form = GetForm();
					return Command.Create(b => b
						.Given(form)
						.Validation(async (name, ct) => name.Length > 0, "required")
						.Then(async (name, ct) => { }));
				}
			}
			""");

		diagnostics.Should().BeEmpty("the runtime type of the parameter cannot be determined");
	}

	[TestMethod]
	public async Task When_GivenLocalOfFeed_Then_Diagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit
			{
				get
				{
					var form = Uno.Extensions.Reactive.Feed.Async(async ct => "");
					return Command.Create(b => b
						.Given(form)
						.Validation(async (name, ct) => name.Length > 0, "required")
						.Then(async (name, ct) => { }));
				}
			}
			""");

		diagnostics.Should().ContainSingle(d => d.Id == "FEED2003");
	}

	[TestMethod]
	public async Task When_GivenOperatorOfState_Then_Diagnostic()
	{
		var diagnostics = await Run("""
			public IAsyncCommand Submit => Command.Create(b => b
				.Given(Name.Select(name => name.Trim()))
				.Validation(async (name, ct) => name.Length > 0, "required")
				.Then(async (name, ct) => { }));
			""");

		diagnostics.Should().ContainSingle(d => d.Id == "FEED2003", "an operator produces a feed, even when applied on a state");
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
