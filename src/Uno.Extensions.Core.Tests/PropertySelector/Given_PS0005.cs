using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Core.Tests.Utils;

namespace Uno.Extensions.Core.Tests.PropertySelector;

[TestClass]
public class Given_PS0005
{
	[TestMethod]
	public void When_Fail()
	{
		var diagnostics = GenerationTestHelper.GetDiagnostics($@"
			using Uno.Extensions.Edition;
			using System.Runtime.CompilerServices;

			namespace TestNamespace;

			public record RootEntity(SubEntity? Child);
			public class SubEntity
			{{
				public string Value {{ get; }}
			}}

			public class SUTClass
			{{
				public void Test()
				{{
					SUTMethod(e => e.Child?.Value);
				}}

				public void SUTMethod(PropertySelector<RootEntity, string> selector, [CallerFilePath] string path = """", [CallerLineNumber] int line = -1)
				{{
				}}
			}}
			");

		diagnostics.Length.Should().Be(1);

		var pathDiag = diagnostics[0];
		pathDiag.Id.Should().Be("PS0005");
		pathDiag.Location.GetLineSpan().StartLinePosition.Line.Should().Be(16);
		pathDiag.Location.GetLineSpan().StartLinePosition.Character.Should().Be(20);
	}
}
