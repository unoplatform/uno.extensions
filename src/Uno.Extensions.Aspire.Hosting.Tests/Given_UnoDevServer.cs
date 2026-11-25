using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.Hosting.Tests;

/// <summary>
/// The dev server the heads connect to for Hot Reload, without running the uno.devserver CLI.
/// </summary>
[TestClass]
public class Given_UnoDevServer
{
	private string _root = "";

	[TestInitialize]
	public void CreateRoot()
	{
		_root = Path.Join(Path.GetTempPath(), $"uno-devserver-{Guid.NewGuid():N}");
		Directory.CreateDirectory(_root);
	}

	[TestCleanup]
	public void DeleteRoot() => Directory.Delete(_root, recursive: true);

	[TestMethod]
	public void When_DiscoveryOutputRead_Then_TheSolutionAndTheRunningServersAreKept()
	{
		var output = """
			Tool package uno.devserver@7.0.29 will be downloaded {from nuget.org}.
			{
			  "selectedSolutionPath": "C:\\src\\MyApp.sln",
			  "hostPath": "C:\\packages\\host.exe",
			  "activeServers": [ { "processId": 42, "port": 5000, "parentProcessId": 7, "solutionPath": "C:\\src\\MyApp.sln", "startTime": "2026-01-01T00:00:00Z" } ]
			}
			""";

		var discovery = UnoDevServer.ParseDiscovery(output);

		discovery.Should().NotBeNull();
		discovery!.SelectedSolutionPath.Should().Be(@"C:\src\MyApp.sln");
		discovery.ActiveServers.Should().ContainSingle().Which.Should().Be(
			new DevServerActiveServer(42, 5000, 7, @"C:\src\MyApp.sln", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
	}

	[TestMethod]
	public void When_DiscoveryOutputHasNoDocument_Then_NothingIsRead()
	{
		UnoDevServer.ParseDiscovery("error: the tool could not be downloaded").Should().BeNull();
	}

	[TestMethod]
	public void When_AServerRunsForTheSolution_Then_ItIsUsed()
	{
		var solution = Path.Join(_root, "MyApp.sln");
		var running = new DevServerActiveServer(42, 5000, 7, solution.ToUpperInvariant());
		var discovery = new DevServerDiscovery(solution, [new DevServerActiveServer(1, 4000, 7, Path.Join(_root, "Other.sln")), running]);

		UnoDevServer.FindRunning(discovery).Should().Be(running);
	}

	[TestMethod]
	public void When_TheSolutionsServerHasNoPort_Then_NoneIsUsed()
	{
		var solution = Path.Join(_root, "MyApp.sln");
		var discovery = new DevServerDiscovery(solution, [new DevServerActiveServer(42, null, 7, solution)]);

		UnoDevServer.FindRunning(discovery).Should().BeNull();
	}

	[TestMethod]
	public void When_NoSolutionWasSelected_Then_NoRunningServerIsUsed()
	{
		var discovery = new DevServerDiscovery(null, [new DevServerActiveServer(42, 5000, 7, Path.Join(_root, "MyApp.sln"))]);

		UnoDevServer.FindRunning(discovery).Should().BeNull();
	}

	[TestMethod]
	public void When_TheCliRuns_Then_ItIsTheVersionOfTheAppsDevServer()
	{
		UnoDevServer.ToolArguments("7.0.29", "start", "--ppid", "7")
			.Should().Equal("dnx", "-y", "uno.devserver@7.0.29", "start", "--ppid", "7");
	}

	[TestMethod]
	public void When_TheAppRestoredTheDevServer_Then_ItsVersionIsRead()
	{
		var assets = Path.Join(_root, "artifacts", "project.assets.json");
		Directory.CreateDirectory(Path.GetDirectoryName(assets)!);
		File.WriteAllText(assets, """{ "libraries": { "Uno.WinUI/7.0.29": {}, "Uno.WinUI.DevServer/7.0.29": {} } }""");

		ProjectAssets.Version(assets, UnoDevServer.DevServerPackageId).Should().Be("7.0.29");
	}

	[TestMethod]
	public void When_TheAssetsFileIsMissing_Then_NoVersionIsRead()
	{
		ProjectAssets.Version(Path.Join(_root, "obj", "project.assets.json"), UnoDevServer.DevServerPackageId).Should().BeNull();
	}

	[TestMethod]
	[DataRow("7.0.29", true)]
	[DataRow("7.1.0-dev.560", true)]
	[DataRow("7.0.29 --add-source http://evil", false)]
	[DataRow("../7.0.29", false)]
	[DataRow("", false)]
	public void When_AVersionIsRead_Then_OnlyAPackageVersionIsPassedToTheCli(string version, bool accepted)
	{
		UnoDevServer.IsPackageVersion(version).Should().Be(accepted);
	}

	[TestMethod]
	[DataRow(null, true)]
	[DataRow("true", true)]
	[DataRow("False", false)]
	public void When_TheSettingIsReadable_Then_ItTurnsTheDevServerOnOrOff(string? setting, bool enabled)
	{
		UnoDevServer.IsEnabled(setting).Should().Be(enabled);
	}

	[TestMethod]
	public void When_TheSettingIsNotABoolean_Then_TheAppHostSaysSo()
	{
		var read = () => UnoDevServer.IsEnabled("no");

		read.Should().Throw<DistributedApplicationException>().WithMessage($"*{UnoDevServer.EnabledSetting}*'no'*");
	}

	[TestMethod]
	public void When_DiscoveryOutputIsMalformed_Then_NothingIsReadAndTheWaitGoesOn()
	{
		UnoDevServer.ParseDiscovery("{ \"selectedSolutionPath\": }\nlater line with a }").Should().BeNull();
	}

	[TestMethod]
	public void When_AServerEntryHasNoPort_Then_TheCompleteOneIsUsed()
	{
		var output = """
			{
			  "selectedSolutionPath": "C:\\src\\MyApp.sln",
			  "activeServers": [
			    { "processId": 41, "solutionPath": "C:\\src\\MyApp.sln" },
			    { "processId": 42, "port": 5000, "solutionPath": "C:\\src\\MyApp.sln", "startTime": "2026-01-01T10:00:00Z" }
			  ]
			}
			""";

		var running = UnoDevServer.FindRunning(UnoDevServer.ParseDiscovery(output)!);

		running.Should().NotBeNull();
		running!.ProcessId.Should().Be(42);
		running.StartTime.Should().Be(new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));
	}

	[TestMethod]
	public void When_TheSolutionIsAboveTheProject_Then_ItsFolderIsWhereTheCliRuns()
	{
		var project = Path.Join(_root, "src", "MyApp");
		Directory.CreateDirectory(project);
		File.WriteAllText(Path.Join(_root, "MyApp.slnx"), "<Solution />");

		UnoDevServer.FindSolutionDirectory(project).Should().Be(_root);
	}

	[TestMethod]
	[Timeout(120_000)]
	public async Task When_TheAppDoesNotReferenceTheDevServer_Then_ItIsUnavailableAndTheHeadsGetNoPort()
	{
		// Restores without the dev server package, so the CLI is never run.
		const string RestoresWithoutIt = """
			<Project>
				<PropertyGroup>
					<ProjectAssetsFile>$(MSBuildProjectDirectory)/obj/project.assets.json</ProjectAssetsFile>
				</PropertyGroup>
				<Target Name="Restore">
					<WriteLinesToFile File="$(ProjectAssetsFile)" Lines="{ &quot;libraries&quot;: { &quot;Other.Package/1.0.0&quot;: {} } }" Overwrite="true" />
				</Target>
			</Project>
			""";
		File.WriteAllText(Path.Join(_root, "MyApp.sln"), "");
		var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = [], ProjectDirectory = _root });
		builder.Configuration["DcpPublisher:CliPath"] = "dcp";
		builder.Configuration["DcpPublisher:DashboardPath"] = "dashboard";
		var devServer = AddProject(builder, "app", projectContent: RestoresWithoutIt).DevServer!;
		await using var app = builder.Build();

		await app.Services.GetRequiredService<IDistributedApplicationEventing>()
			.PublishAsync(new BeforeStartEvent(app.Services, app.Services.GetRequiredService<DistributedApplicationModel>()));

		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
		var state = await app.Services.GetRequiredService<ResourceNotificationService>().WaitForResourceAsync(
			devServer.Name, resource => resource.Snapshot.State?.Text is "Unavailable" or "FailedToStart", timeout.Token);

		state.Snapshot.State?.Text.Should().Be("Unavailable");
		state.Snapshot.Properties.Should().Contain(property =>
			property.Name == "Reason" && ((string)property.Value!).Contains($"does not reference {UnoDevServer.DevServerPackageId}"));
		(await devServer.Port.WaitAsync(TimeSpan.FromSeconds(10))).Should().BeNull();
		devServer.IsStarting.Should().BeTrue();
	}

	[TestMethod]
	public void When_TwoAppsShareASolution_Then_TheyShareOneDevServer()
	{
		var builder = DistributedApplication.CreateBuilder();
		File.WriteAllText(Path.Join(_root, "MyApp.sln"), "");

		var app = AddProject(builder, "app");
		var other = AddProject(builder, "other");

		builder.Resources.OfType<UnoDevServerResource>().Should().ContainSingle().Which.Name.Should().Be(UnoDevServer.ResourceName);
		other.DevServer.Should().BeSameAs(app.DevServer);
	}

	[TestMethod]
	public void When_AppsAreInTwoSolutions_Then_EachSolutionHasItsDevServer()
	{
		var builder = DistributedApplication.CreateBuilder();

		var first = AddProject(builder, "first", solution: "First");
		var second = AddProject(builder, "second", solution: "Second");

		builder.Resources.OfType<UnoDevServerResource>().Select(devServer => devServer.Name)
			.Should().BeEquivalentTo(UnoDevServer.ResourceName, $"{UnoDevServer.ResourceName}-2");
		second.DevServer.Should().NotBeSameAs(first.DevServer);
	}

	[TestMethod]
	public void When_TheDevServerIsTurnedOff_Then_NoneIsAdded()
	{
		var builder = DistributedApplication.CreateBuilder();
		builder.Configuration[UnoDevServer.EnabledSetting] = "false";

		var app = AddProject(builder, "app");

		builder.Resources.OfType<UnoDevServerResource>().Should().BeEmpty();
		app.DevServer.Should().BeNull();
	}

	[TestMethod]
	public void When_AnotherResourceHasTheDevServersName_Then_AddingTheAppSaysSo()
	{
		var builder = DistributedApplication.CreateBuilder();
		builder.AddParameter(UnoDevServer.ResourceName);

		var add = () => AddProject(builder, "app");

		add.Should().Throw<DistributedApplicationException>().WithMessage($"*'{UnoDevServer.ResourceName}'*{UnoDevServer.EnabledSetting}=false*");
	}

	[TestMethod]
	public void When_Publishing_Then_NoDevServerIsAdded()
	{
		var builder = DistributedApplication.CreateBuilder(["--publisher", "manifest", "--output-path", Path.Join(_root, "manifest")]);

		var app = AddProject(builder, "app");

		builder.Resources.OfType<UnoDevServerResource>().Should().BeEmpty();
		app.DevServer.Should().BeNull();
	}

	// A project under _root, in a solution folder of its own when one is named.
	private UnoProjectResource AddProject(IDistributedApplicationBuilder? builder, string name, string? solution = null, string projectContent = "<Project />")
	{
		var folder = solution is null ? _root : Path.Join(_root, solution);
		if (solution is not null)
		{
			Directory.CreateDirectory(folder);
			File.WriteAllText(Path.Join(folder, $"{solution}.sln"), "");
		}

		var projectPath = Path.Join(folder, name, $"{name}.csproj");
		Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
		File.WriteAllText(projectPath, projectContent);

		builder ??= DistributedApplication.CreateBuilder();
		return builder.AddUnoProject(name, projectPath).Resource;
	}
}
