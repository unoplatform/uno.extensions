using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Core.Tests.Utils;

namespace Uno.Extensions.Core.Tests.PropertySelector;

[TestClass]
public class Given_PS0003
{
	[TestMethod]
	public async Task When_UsingMethodGroup()
	{
		// Note: This rule is only a restriction of the current implementation, we could support more syntax in the future.

		var compilation = GenerationTestHelper.CreateCompilationWithAnalyzers($@"
			using Uno.Extensions.Edition;
			using System.Runtime.CompilerServices;

			namespace TestNamespace;

			public record Entity(string Value);

			public class SUTClass
			{{
				public void Test()
				{{
					SUTMethod(GetValue);
				}}

				public static string GetValue(Entity e) => e.Value;

				public void SUTMethod(PropertySelector<Entity, string> selector, [CallerFilePath] string path = """", [CallerLineNumber] int line = -1)
				{{
				}}
			}}
			");

		var diagnostics = await compilation.GetAnalyzerDiagnosticsAsync();
		diagnostics.Length.Should().Be(1);

		var pathDiag = diagnostics[0];
		pathDiag.Id.Should().Be("PS0003");
		pathDiag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(12);
		pathDiag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(15);

		GenerationTestHelper.RunGeneratorTwice(
			compilation.Compilation,
			run1 => GenerationTestHelper.AssertRunReason(run1, IncrementalStepRunReason.New, expectedTrackedStepsCount: 0),
			run2 => GenerationTestHelper.AssertRunReason(run2, IncrementalStepRunReason.Cached, expectedTrackedStepsCount: 0),
			null);
	}
}
