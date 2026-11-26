using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.Hosting.Tests;

[TestClass]
public class Given_WebAssemblyHead
{
	private readonly TestSolution _solution = new();

	[TestCleanup]
	public void DeleteSolution() => _solution.Dispose();

	[TestMethod]
	public async Task When_Started_Then_ItRunsTheBrowserFrameworkOnTheAppHostPort()
	{
		var builder = _solution.CreateBuilder();

		var wasm = _solution.AddWebAssembly(builder);

		wasm.Resource.Name.Should().Be("app-wasm");
		var result = await ExecutionConfiguration.ResolveAsync(builder, wasm.Resource);
		result.Arguments.Select(argument => argument.Value).Should()
			.Equal("run", "--project", _solution.ProjectPath, "-f", TestSolution.WebAssemblyTargetFramework, "--no-build", "--no-launch-profile");
		result.EnvironmentVariables.Should().Contain("ASPNETCORE_URLS", "http://localhost:5377");
		result.EnvironmentVariables.Should().NotContainKey("OTEL_EXPORTER_OTLP_ENDPOINT");
	}

	[TestMethod]
	public void When_Added_Then_ItIsStartedByHandIsHealthCheckedAndStaysOutOfTheManifest()
	{
		var builder = _solution.CreateBuilder();

		var wasm = _solution.AddWebAssembly(builder);

		wasm.Resource.Annotations.OfType<ExplicitStartupAnnotation>().Should().ContainSingle();
		wasm.Resource.Annotations.OfType<HealthCheckAnnotation>().Should().ContainSingle();
		wasm.Resource.Annotations.OfType<ManifestPublishingCallbackAnnotation>().Should().Equal(ManifestPublishingCallbackAnnotation.Ignore);
	}
}
