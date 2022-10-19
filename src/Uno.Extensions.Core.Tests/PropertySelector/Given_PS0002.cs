using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Core.Tests.Utils;

namespace Uno.Extensions.Core.Tests.PropertySelector;

[TestClass]
public class Given_PS0002
{
	[TestMethod]
	public void When_UsingExternalVariable()
	{
		var diagnostics = GenerationTestHelper.GetDiagnostics($@"
			using Uno.Extensions.Edition;
			using System.Runtime.CompilerServices;

			namespace TestNamespace;

			public record Entity(string Value);

			public class SUTClass
			{{
				public void Test()
				{{
					var external = new Entity(""hello world"");
					SUTMethod(e => external.Value);
				}}

				public void SUTMethod(PropertySelector<Entity, string> selector, [CallerFilePath] string path = """", [CallerLineNumber] int line = -1)
				{{
				}}
			}}
			");

		diagnostics.Length.Should().Be(1);

		var pathDiag = diagnostics[0];
		pathDiag.Id.Should().Be("PS0002");
		pathDiag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(13);
		pathDiag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(20);
	}
}
