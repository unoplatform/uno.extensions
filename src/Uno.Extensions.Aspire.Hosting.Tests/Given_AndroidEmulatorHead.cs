using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.Hosting.Tests;

[TestClass]
public class Given_AndroidEmulatorHead
{
	private readonly TestSolution _solution = new();

	[TestCleanup]
	public void DeleteSolution() => _solution.Dispose();

	[TestMethod]
	public async Task When_Started_Then_ItDeploysTheAndroidFrameworkToTheEmulator()
	{
		var builder = _solution.CreateBuilder();

		var android = _solution.AddAndroidEmulator(builder);

		android.Resource.Name.Should().Be("app-android");
		var result = await TestSolution.StartAsync(builder, android.Resource);
		result.Arguments.Select(argument => argument.Value).Should()
			.Equal("run", "--project", _solution.ProjectPath, "-f", TestSolution.AndroidTargetFramework, "--no-launch-profile", "-p:AdbTarget=-e");
		result.EnvironmentVariables.Should().NotContainKey("OTEL_EXPORTER_OTLP_ENDPOINT");
		android.Resource.Annotations.OfType<ExplicitStartupAnnotation>().Should().ContainSingle();
		android.Resource.Annotations.OfType<ManifestPublishingCallbackAnnotation>().Should().Equal(ManifestPublishingCallbackAnnotation.Ignore);
	}
}
