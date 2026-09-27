using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Configuration.Tests;

/// <summary>
/// Guards <see cref="ConfigBuilderExtensions.EmbeddedSource{TApplicationRoot}"/> with a named
/// configuration: the environment-specific file (<c>appsettings.{name}.{environment}.json</c>)
/// must be read from the embedded resources, like the base file, and not from content files.
/// </summary>
/// <remarks>
/// Regression test for https://github.com/unoplatform/uno.extensions/issues/2681.
/// </remarks>
[TestClass]
public class Given_EmbeddedSource
{
	private const string ConfigurationName = "custom";
	private const string MarkerKey = "CustomMarker";

	// A type in THIS test assembly, which embeds appsettings.custom.json and
	// appsettings.custom.development.json (see the .csproj).
	private sealed class AppRoot
	{
	}

	[TestMethod]
	public void When_NamedWithEnvironmentSettings_Then_EmbeddedEnvironmentFileOverridesBase()
	{
		var marker = ReadMarker(includeEnvironmentSettings: true);

		marker.Should().Be(
			"development",
			"appsettings.custom.development.json is embedded and must override appsettings.custom.json");
	}

	[TestMethod]
	public void When_NamedWithoutEnvironmentSettings_Then_OnlyBaseFileIsRead()
	{
		var marker = ReadMarker(includeEnvironmentSettings: false);

		marker.Should().Be("base");
	}

	[TestMethod]
	public void When_NamedInOtherEnvironment_Then_OnlyBaseFileIsRead()
	{
		var marker = ReadMarker(includeEnvironmentSettings: true, Environments.Staging);

		marker.Should().Be(
			"base",
			"only appsettings.custom.development.json is embedded, so no file matches the Staging environment");
	}

	private static string? ReadMarker(bool includeEnvironmentSettings, string environment = "Development")
	{
		using var host = new HostBuilder()
			.UseEnvironment(environment)
			// The Development environment validates every registration on Build(); UseConfiguration's
			// ReloadService needs IStorage, which is out of scope here. The host is never started, and
			// only IConfiguration is resolved.
			.UseDefaultServiceProvider(options => options.ValidateOnBuild = false)
			.UseConfiguration(configure: config => config.EmbeddedSource<AppRoot>(ConfigurationName, includeEnvironmentSettings))
			.Build();

		return host.Services.GetRequiredService<IConfiguration>()[MarkerKey];
	}
}
