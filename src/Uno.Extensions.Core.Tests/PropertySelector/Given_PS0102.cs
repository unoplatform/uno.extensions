using System;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Core.Tests.Utils;

namespace Uno.Extensions.Core.Tests.PropertySelector;

[TestClass]
public class Given_PS0102
{
	[TestMethod]
	public void When_Path()
	{
		var diagnostics = TestInvoke(@"
			var path = ""42"";
			SUTMethod(e => e.Value, path);");

		diagnostics.Length.Should().Be(1);
		var diag = diagnostics[0];

		diag.Id.Should().Be("PS0102");
		diag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(14);
		diag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(27);
	}

	[TestMethod]
	public void When_Line()
	{
		var diagnostics = TestInvoke(@"
			var line = 42;
			SUTMethod(e => e.Value, line: line);");

		diagnostics.Length.Should().Be(1);
		var diag = diagnostics[0];

		diag.Id.Should().Be("PS0102");
		diag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(14);
		diag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(33);
	}

	[TestMethod]
	public void When_PathAndLine()
	{
		var diagnostics = TestInvoke(@"
			var path = ""42"";
			var line = 42;
			SUTMethod(e => e.Value, path, line);");

		diagnostics.Length.Should().Be(1);

		var pathDiag = diagnostics[0];
		pathDiag.Id.Should().Be("PS0102");
		pathDiag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(15);
		pathDiag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(27);

		//var lineDiag = diagnostics[1];
		//lineDiag.Id.Should().Be("PS0102");
		//lineDiag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(15);
		//lineDiag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(33);
	}

	private ImmutableArray<Diagnostic> TestInvoke(string invocation)
		=> GenerationTestHelper.GetDiagnostics($@"
			using Uno.Extensions.Edition;
			using System.Runtime.CompilerServices;

			namespace TestNamespace;

			public record Entity(string Value);

			public class SUTClass
			{{
				public void Test()
				{{
					{invocation}
				}}

				public void SUTMethod(PropertySelector<Entity, string> selector, [CallerFilePath] string path = """", [CallerLineNumber] int line = -1)
				{{
				}}
			}}
			");
}
