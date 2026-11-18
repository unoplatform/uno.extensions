using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

	[TestMethod]
	public async Task When_ReferencesAndSecretsAreSet_Then_OnlyEndpointAddressesAreBaked()
	{
		var builder = _solution.CreateBuilder();
		var api = TestSolution.AddApi(builder);
		var secret = builder.AddParameter("secret", "s3cret", secret: true);
		var database = builder.AddConnectionString("db", ReferenceExpression.Create($"Host=localhost;Password={secret.Resource}"));
		var logger = new ListLogger();

		var wasm = _solution.AddWebAssembly(builder)
			.WithReference(api.GetEndpoint("http"))
			.WithReference(database)
			.WithEnvironment("ApiKey", secret)
			.WithEnvironment("Literal", "value")
			.WithEnvironment("services__wrapped__http__0", ReferenceExpression.Create($"{api.GetEndpoint("http")}?key={secret.Resource}"));
		await TestSolution.WriteClientEnvironmentAsync(builder, wasm.Resource, logger);

		_solution.BakedVariables().Should().Equal(new Dictionary<string, string> { ["services__api__http__0"] = "http://localhost:5171" });
		logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Warning)
			.Which.Message.Should().Contain("services__wrapped__http__0");
		logger.Entries.Should().Contain(entry => entry.Level == LogLevel.Debug && entry.Message.Contains("ApiKey") && entry.Message.Contains("Literal"));
		string.Concat(logger.Entries.Select(entry => entry.Message)).Should().NotContain("s3cret");
	}

	[TestMethod]
	public async Task When_ProjectReferenced_Then_ItsAddressIsBakedWithoutWarning()
	{
		var builder = _solution.CreateBuilder();
#pragma warning disable CS0618 // Moved to Aspire.Hosting.Dotnet in Aspire 17; the package supports 13.6.5, where it is here.
		var api = builder.AddProject("api", _solution.ProjectPath, launchProfileName: null)
#pragma warning restore CS0618
			.WithHttpEndpoint(name: "http")
			.WithEndpoint("http", endpoint => endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, "localhost", 5171));
		var logger = new ListLogger();

		var wasm = _solution.AddWebAssembly(builder).WithReference(api);
		await TestSolution.WriteClientEnvironmentAsync(builder, wasm.Resource, logger);

		_solution.BakedVariables().Should().Equal(new Dictionary<string, string> { ["services__api__http__0"] = "http://localhost:5171" });
		logger.Entries.Should().NotContain(entry => entry.Level >= LogLevel.Warning);
	}

	[TestMethod]
	public async Task When_VariablesOptedIn_Then_TheyAreBakedAndSecretsOrMissingNamesWarn()
	{
		var builder = _solution.CreateBuilder();
		var secret = builder.AddParameter("secret", "s3cret", secret: true);
		var logger = new ListLogger();

		var wasm = _solution.AddWebAssembly(builder)
			.WithEnvironment("Feature", "on")
			.WithEnvironment("Token", secret)
			.WithClientEnvironment("Feature")
			.WithClientEnvironment("Token")
			.WithClientEnvironment("Typo");
		await TestSolution.WriteClientEnvironmentAsync(builder, wasm.Resource, logger);

		_solution.BakedVariables().Should().Equal(new Dictionary<string, string> { ["Feature"] = "on", ["Token"] = "s3cret" });
		var warnings = logger.Entries.Where(entry => entry.Level == LogLevel.Warning).Select(entry => entry.Message).ToList();
		warnings.Should().HaveCount(2);
		warnings.Should().Contain(message => message.Contains("Typo"));
		warnings.Should().Contain(message => message.Contains("Token") && message.Contains("secret"));
	}

	[TestMethod]
	public async Task When_ValuesChangeBetweenRuns_Then_TheFileIsRewrittenOnlyWhenTheyChange()
	{
		async Task RunAsync(string value)
		{
			var builder = _solution.CreateBuilder();
			var wasm = _solution.AddWebAssembly(builder).WithEnvironment("Value", value).WithClientEnvironment("Value");
			await TestSolution.WriteClientEnvironmentAsync(builder, wasm.Resource);
		}

		await RunAsync("one");
		var written = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		File.SetLastWriteTimeUtc(_solution.ValuesPath, written);

		await RunAsync("one");
		File.GetLastWriteTimeUtc(_solution.ValuesPath).Should().Be(written, "an unchanged value keeps the build incremental");

		await RunAsync("two");
		_solution.BakedVariables().Should().Contain("Value", "two");
		Directory.GetFiles(Path.GetDirectoryName(_solution.ValuesPath)!).Should().ContainSingle("no temporary file is left behind");
	}

	[TestMethod]
	[DataRow("Name", "line\nbreak")]
	[DataRow("not-an-identifier", "value")]
	[DataRow("1Name", "value")]
	public async Task When_VariableCannotBeWrittenSafely_Then_TheStartFails(string name, string value)
	{
		var builder = _solution.CreateBuilder();
		var wasm = _solution.AddWebAssembly(builder).WithEnvironment(name, value).WithClientEnvironment(name);

		var start = async () =>
		{
			await TestSolution.WriteClientEnvironmentAsync(builder, wasm.Resource);
		};

		await start.Should().ThrowAsync<DistributedApplicationException>().WithMessage($"*'{name}'*'app-wasm'*");
	}

	[TestMethod]
	public async Task When_AppHostStartsAndStops_Then_TheValuesAndTemporaryFilesAreRemoved()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(_solution.ValuesPath)!);
		File.WriteAllText(_solution.ValuesPath, "<Project />");
		File.WriteAllText($"{_solution.ValuesPath}.abc.tmp", "<Project />");
		var builder = _solution.CreateBuilder();
		// Aspire's own start subscribers check these paths; nothing is started.
		builder.Configuration["DcpPublisher:CliPath"] = "dcp";
		builder.Configuration["DcpPublisher:DashboardPath"] = "dashboard";
		var wasm = _solution.AddWebAssembly(builder);
		await using var app = builder.Build();

		await app.Services.GetRequiredService<IDistributedApplicationEventing>()
			.PublishAsync(new BeforeStartEvent(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

		Directory.GetFiles(Path.GetDirectoryName(_solution.ValuesPath)!).Should().BeEmpty("a file left by an AppHost that was killed holds a dead address");
		await TestSolution.WriteClientEnvironmentAsync(builder, wasm.Resource);

		app.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();

		File.Exists(_solution.ValuesPath).Should().BeFalse();
	}

	[TestMethod]
	[DataRow("Uno.Extensions.Aspire/1.0.0", false, DisplayName = "The app references the client package")]
	[DataRow("Uno.Extensions.Aspire.Hosting/1.0.0", true, DisplayName = "The app references another package only")]
	public async Task When_AppWasRestored_Then_AMissingClientPackageWarns(string library, bool warns)
	{
		// The decoy under "targets" must not count: only "libraries" lists what was restored.
		Directory.CreateDirectory(Path.Join(_solution.AppFolder, "obj"));
		File.WriteAllText(Path.Join(_solution.AppFolder, "obj", "project.assets.json"), $$"""
			{
			  "version": 3,
			  "targets": { "{{TestSolution.WebAssemblyTargetFramework}}": { "Uno.Extensions.Aspire/0.0.0": {} } },
			  "libraries": { "Other.Package/1.0.0": { "type": "package" }, "{{library}}": { "type": "package" } }
			}
			""");
		var builder = _solution.CreateBuilder();
		var logger = new ListLogger();

		var wasm = _solution.AddWebAssembly(builder);
		await TestSolution.WriteClientEnvironmentAsync(builder, wasm.Resource, logger);

		var warnings = logger.Entries.Where(entry => entry.Level == LogLevel.Warning).Select(entry => entry.Message);
		if (warns)
		{
			warnings.Should().ContainSingle().Which.Should().Contain("does not reference Uno.Extensions.Aspire");
		}
		else
		{
			warnings.Should().BeEmpty();
		}
	}

	[TestMethod]
	public async Task When_TheEnvironmentFailsToResolve_Then_TheLastValuesStay()
	{
		var first = _solution.CreateBuilder();
		var wasm = _solution.AddWebAssembly(first).WithEnvironment("Value", "one").WithClientEnvironment("Value");
		await TestSolution.WriteClientEnvironmentAsync(first, wasm.Resource);
		var written = File.ReadAllText(_solution.ValuesPath);
		var second = _solution.CreateBuilder();
		var unset = second.AddParameter("unset");
		var failing = _solution.AddWebAssembly(second)
			.WithEnvironment("Value", unset)
			.WithClientEnvironment("Value");

		var start = () => TestSolution.WriteClientEnvironmentAsync(second, failing.Resource);

		await start.Should().ThrowAsync<DistributedApplicationException>().WithMessage("*'app-wasm'*nothing was written*");
		File.ReadAllText(_solution.ValuesPath).Should().Be(written);
	}

	[TestMethod]
	public async Task When_HeadStarts_Then_ItsBuildCarriesTheValues()
	{
		var builder = _solution.CreateBuilder();
		// Aspire's own start subscribers check these paths; nothing is started.
		builder.Configuration["DcpPublisher:CliPath"] = "dcp";
		builder.Configuration["DcpPublisher:DashboardPath"] = "dashboard";
		var api = TestSolution.AddApi(builder);
		var wasm = _solution.AddWebAssembly(builder).WithReference(api.GetEndpoint("http"));
		await using var app = builder.Build();

		await app.Services.GetRequiredService<IDistributedApplicationEventing>()
			.PublishAsync(new BeforeResourceStartedEvent(wasm.Resource, app.Services));

		File.ReadAllLines(Path.Join(_solution.AppFolder, "environment.txt")).Should().Equal("services__api__http__0=http://localhost:5171");
	}

	[TestMethod]
	public void When_NothingWasWritten_Then_RemovingIsSilent()
	{
		var builder = _solution.CreateBuilder();
		var wasm = _solution.AddWebAssembly(builder);
		var logger = new ListLogger();

		BakedEnvironment.Delete(wasm.Resource, logger);

		logger.Entries.Should().BeEmpty("a first run has no obj/uno-aspire folder yet");
	}
}
