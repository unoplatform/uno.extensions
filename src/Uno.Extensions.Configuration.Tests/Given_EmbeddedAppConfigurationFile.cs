using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Configuration;

namespace Uno.Extensions.Configuration.Tests;

/// <summary>
/// Guards <see cref="EmbeddedAppConfigurationFile.AllFiles{TApplicationRoot}"/> against the
/// first-caller-wins static cache that served the first application's <c>appsettings.*</c> files
/// to every later, unrelated assembly.
/// </summary>
/// <remarks>
/// This reproduces the topology of a downstream host that loads previewed apps into their own
/// collectible <c>AssemblyLoadContext</c>s: distinct application-root types resolve to distinct
/// assemblies, and each must receive only its own embedded configuration files.
/// </remarks>
[TestClass]
public class Given_EmbeddedAppConfigurationFile
{
	// A type in THIS test assembly, which embeds exactly one appsettings.json (see the .csproj).
	private sealed class AppRootWithSettings
	{
	}

	[TestMethod]
	public void When_SecondAssemblyRequested_Then_DoesNotReturnFirstAssemblyFiles()
	{
		// Arrange / Act — prime the cache with the assembly that HAS an appsettings resource first.
		var withSettings = EmbeddedAppConfigurationFile.AllFiles<AppRootWithSettings>();

		// A framework type resolves to an assembly with no embedded "appsettings" resource.
		var withoutSettings = EmbeddedAppConfigurationFile.AllFiles<StringBuilder>();

		// Assert — the second, unrelated assembly must NOT inherit the first assembly's files.
		// On the buggy first-caller-wins cache this returned the AppRootWithSettings file(s).
		withSettings.Should().NotBeEmpty("this assembly embeds appsettings.json");
		withSettings.Should().OnlyContain(file => file.FileName.Contains("appsettings"));

		withoutSettings.Should().BeEmpty(
			"a second application's assembly must resolve its own configuration, not the first caller's");
	}

	[TestMethod]
	public void When_FirstRequestedIsWithoutSettings_Then_SecondAssemblyStillGetsItsOwnFiles()
	{
		// Arrange / Act — reversed order: prime with the assembly that has NO appsettings first.
		var withoutSettings = EmbeddedAppConfigurationFile.AllFiles<Uri>();

		// Then request this assembly, which DOES embed appsettings.json.
		var withSettings = EmbeddedAppConfigurationFile.AllFiles<AppRootWithSettings>();

		// Assert — order must not matter: each assembly resolves its own files.
		withoutSettings.Should().BeEmpty("the framework assembly embeds no appsettings resource");
		withSettings.Should().NotBeEmpty("this assembly embeds appsettings.json");
		withSettings.Should().OnlyContain(file => file.FileName.Contains("appsettings"));
	}

	[TestMethod]
	public void When_SameAssemblyRequestedTwice_Then_ReturnsCachedInstance()
	{
		// The per-assembly cache must still be a cache: repeated calls for the same assembly
		// return the identical array instance (no re-scan of manifest resources).
		var first = EmbeddedAppConfigurationFile.AllFiles<AppRootWithSettings>();
		var second = EmbeddedAppConfigurationFile.AllFiles<AppRootWithSettings>();

		second.Should().BeSameAs(first);
	}
}
