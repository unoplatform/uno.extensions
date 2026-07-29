using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Core.Tests.PlatformHelper;

/// <summary>
/// Guards that <see cref="global::Uno.Extensions.PlatformHelper.SetAppAssembly"/> does not pin the
/// supplied assembly for the process lifetime.
/// </summary>
/// <remarks>
/// A downstream host that loads previewed apps into collectible <see cref="AssemblyLoadContext"/>s
/// calls <c>SetAppAssembly</c> with the previewed app's assembly. If <c>PlatformHelper</c> held that
/// assembly with a strong reference, the collectible ALC could never be unloaded. The reference is
/// held weakly instead, so once the ALC drops its own references the assembly (and its ALC) become
/// collectible.
/// </remarks>
[TestClass]
public class Given_PlatformHelper
{
	[TestCleanup]
	public void Cleanup()
	{
		// Do not leak state between tests / suites.
		global::Uno.Extensions.PlatformHelper.SetAppAssembly(null);
	}

	[TestMethod]
	public void When_SetAppAssembly_From_CollectibleAlc_Then_AlcIsCollectible()
	{
		// Arrange — emit and load a tiny assembly into a collectible ALC, then register it.
		var alcReference = LoadAndRegisterInCollectibleAlc();

		// Act — give the runtime a chance to unload the ALC now that no strong reference remains
		// (only PlatformHelper's internal reference, which must be weak).
		for (var i = 0; i < 10 && alcReference.IsAlive; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		// Assert — with a weak internal reference the ALC unloads; with a strong one it stays alive.
		alcReference.IsAlive.Should().BeFalse(
			"PlatformHelper must not hold a strong reference that pins the previewed app's collectible ALC");
	}

	// Kept in a separate non-inlined frame so the ALC + assembly locals are not kept alive by the
	// caller's stack while we wait for collection.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference LoadAndRegisterInCollectibleAlc()
	{
		var image = EmitTinyAssembly();

		var alc = new AssemblyLoadContext(nameof(Given_PlatformHelper), isCollectible: true);
		using (var stream = new MemoryStream(image))
		{
			var assembly = alc.LoadFromStream(stream);
			global::Uno.Extensions.PlatformHelper.SetAppAssembly(assembly);
		}

		var reference = new WeakReference(alc, trackResurrection: false);
		alc.Unload();
		return reference;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static byte[] EmitTinyAssembly()
	{
		var syntaxTree = CSharpSyntaxTree.ParseText("namespace HostedPreviewApp { public sealed class App { } }");
		var runtime = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
		var compilation = CSharpCompilation.Create(
			assemblyName: "HostedPreviewApp",
			syntaxTrees: new[] { syntaxTree },
			references: new[] { runtime },
			options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		using var peStream = new MemoryStream();
		var result = compilation.Emit(peStream);
		result.Success.Should().BeTrue("the test fixture assembly must compile");
		return peStream.ToArray();
	}
}
