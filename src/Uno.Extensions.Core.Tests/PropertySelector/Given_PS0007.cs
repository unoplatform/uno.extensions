using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Core.Tests.Utils;

namespace Uno.Extensions.Core.Tests.PropertySelector;

[TestClass]
public class Given_PS0007
{
	[TestMethod]
	public async Task When_MissingPropertySelectorAttribute()
	{
		var compilation = GenerationTestHelper.CreateCompilationWithAnalyzers($@"
			using Uno.Extensions.Edition;
			using System.Runtime.CompilerServices;

			namespace TestNamespace;

			public record Entity(string Value);

			public class SUTClass
			{{
				public void Test()
				{{
					SUTMethod(e => e.ToString());
				}}

				public void SUTMethod(PropertySelector<Entity, string> selector, [CallerFilePath] string path = """", [CallerLineNumber] int line = -1)
				{{
				}}
			}}
			");

		var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync();
		diagnostics.Length.Should().Be(1);

		var pathDiag = diagnostics[0];
		pathDiag.Id.Should().Be("PS0007");
		pathDiag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(12);
		pathDiag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(15);

		GenerationTestHelper.RunGeneratorTwice(
			compilation.Compilation,
			run1 => GenerationTestHelper.AssertRunReason(run1, IncrementalStepRunReason.New, expectedTrackedStepsCount: 0),
			run2 => GenerationTestHelper.AssertRunReason(run2, IncrementalStepRunReason.Cached, expectedTrackedStepsCount: 0),
			null);
	}
}
