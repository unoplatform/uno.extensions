using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.Hosting.Tests;

[TestClass]
public class Given_AndroidEmulatorHead
{
	private readonly TestSolution _solution = new();

	[TestCleanup]
	public void DeleteSolution() => _solution.Dispose();

	private static IResourceBuilder<ExecutableResource> AddApiWithHttps(IDistributedApplicationBuilder builder) =>
		TestSolution.AddApi(builder)
			.WithHttpsEndpoint(name: "https")
			.WithEndpoint("https", endpoint => endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 7171));

	[TestMethod]
	public async Task When_Started_Then_ItDeploysTheAndroidFrameworkToTheEmulator()
	{
		var builder = _solution.CreateBuilder();

		var android = _solution.AddAndroidEmulator(builder);

		android.Resource.Name.Should().Be("app-android");
		var result = await ExecutionConfiguration.ResolveAsync(builder, android.Resource);
		result.Arguments.Select(argument => argument.Value).Should()
			.Equal("run", "--project", _solution.ProjectPath, "-f", TestSolution.AndroidTargetFramework, "--no-build", "--no-launch-profile", "-p:AdbTarget=-e");
		result.EnvironmentVariables.Should().NotContainKey("OTEL_EXPORTER_OTLP_ENDPOINT");
		android.Resource.Annotations.OfType<ExplicitStartupAnnotation>().Should().ContainSingle();
		android.Resource.Annotations.OfType<ManifestPublishingCallbackAnnotation>().Should().Equal(ManifestPublishingCallbackAnnotation.Ignore);
	}

	[TestMethod]
	public async Task When_ApiIsReferenced_Then_OnlyItsHttpAddressIsBakedForTheEmulator()
	{
		var builder = _solution.CreateBuilder();
		var api = AddApiWithHttps(builder);
		var logger = new ListLogger();

		var android = _solution.AddAndroidEmulator(builder)
			.WithReference(api.GetEndpoint("http"))
			.WithReference(api.GetEndpoint("https"));
		await TestSolution.WriteClientEnvironmentAsync(builder, android.Resource, logger);

		_solution.BakedVariables(TestSolution.AndroidTargetFramework).Should()
			.Equal(new Dictionary<string, string> { ["SERVICES__API__HTTP__0"] = "http://10.0.2.2:5171" });
		logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Information && entry.Message.Contains("services__api__https__0"));
		logger.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning);
	}

	[TestMethod]
	public async Task When_ApiOffersOnlyHttps_Then_TheHeadWarnsItGetsNoAddress()
	{
		var builder = _solution.CreateBuilder();
		var api = builder.AddExecutable("api", "api", ".")
			.WithHttpsEndpoint(name: "https")
			.WithEndpoint("https", endpoint => endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 7171));
		var logger = new ListLogger();

		var android = _solution.AddAndroidEmulator(builder).WithReference(api.GetEndpoint("https"));
		await TestSolution.WriteClientEnvironmentAsync(builder, android.Resource, logger);

		_solution.BakedVariables(TestSolution.AndroidTargetFramework).Should().BeEmpty();
		logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning)
			.Which.Message.Should().Contain("no address for api");
	}

	[TestMethod]
	public async Task When_ServiceVariableOptedIn_Then_ItIsTreatedAsAServiceAddress()
	{
		var builder = _solution.CreateBuilder();

		var android = _solution.AddAndroidEmulator(builder)
			.WithEnvironment("services__legacy__http__0", "http://localhost:6000")
			.WithClientEnvironment("services__legacy__http__0");
		await TestSolution.WriteClientEnvironmentAsync(builder, android.Resource);

		_solution.BakedVariables(TestSolution.AndroidTargetFramework).Should()
			.Equal(new Dictionary<string, string> { ["SERVICES__LEGACY__HTTP__0"] = "http://10.0.2.2:6000" });
	}

	[TestMethod]
	public async Task When_VariableOptedIn_Then_ItIsBakedUnchangedButUppercased()
	{
		var builder = _solution.CreateBuilder();

		var android = _solution.AddAndroidEmulator(builder)
			.WithEnvironment("Feature__Url", "http://localhost:1234/")
			.WithClientEnvironment("Feature__Url");
		await TestSolution.WriteClientEnvironmentAsync(builder, android.Resource);

		_solution.BakedVariables(TestSolution.AndroidTargetFramework).Should()
			.Equal(new Dictionary<string, string> { ["FEATURE__URL"] = "http://localhost:1234/" });
	}

	[TestMethod]
	public async Task When_TwoNamesDifferOnlyByCase_Then_TheStartFails()
	{
		var builder = _solution.CreateBuilder();
		var android = _solution.AddAndroidEmulator(builder)
			.WithEnvironment("Name", "one")
			.WithEnvironment("NAME", "two")
			.WithClientEnvironment("Name")
			.WithClientEnvironment("NAME");

		var start = async () =>
		{
			await TestSolution.WriteClientEnvironmentAsync(builder, android.Resource);
		};

		await start.Should().ThrowAsync<DistributedApplicationException>().WithMessage("*only by case*");
	}

	[TestMethod]
	[DataRow("http://localhost:5171", "http://10.0.2.2:5171")]
	[DataRow("http://127.0.0.1:5171/", "http://10.0.2.2:5171/")]
	[DataRow("http://[::1]:5171", "http://10.0.2.2:5171")]
	[DataRow("http://api.internal:5171", "http://api.internal:5171")]
	[DataRow("https://api.example.devtunnels.ms", "https://api.example.devtunnels.ms", DisplayName = "A trusted https address stays")]
	[DataRow("http://localhost:5171/a%20b", "http://10.0.2.2:5171/a%20b", DisplayName = "An encoded path stays encoded")]
	public void When_ServiceAddressIsPlanned_Then_LoopbackBecomesTheEmulatorHost(string address, string expected)
	{
		var plan = new BakePlan(new Dictionary<string, string> { ["services__api__http__0"] = address }, [], [], [], []);

		var planned = BakedEnvironment.ForAndroidEmulator(plan);

		planned.Variables.Should().Equal(new Dictionary<string, string> { ["SERVICES__API__HTTP__0"] = expected });
	}
}
