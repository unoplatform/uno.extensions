using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.Hosting.Tests;

/// <summary>
/// The build that runs before a head starts, with a real <c>dotnet build</c> of a project that builds instantly.
/// </summary>
[TestClass]
public class Given_HeadBuild
{
	private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);

	private TestSolution? _solution;

	[TestCleanup]
	public void DeleteSolution() => _solution?.Dispose();

	private (DistributedApplication App, UnoDesktopResource Head) CreateApp(string projectContent = TestSolution.BuildTargets)
	{
		_solution = new TestSolution(projectContent);
		var builder = _solution.CreateBuilder();
		// Aspire's own start subscribers check these paths; nothing is started.
		builder.Configuration["DcpPublisher:CliPath"] = "dcp";
		builder.Configuration["DcpPublisher:DashboardPath"] = "dashboard";
		var head = builder.AddUnoProject("app", _solution.ProjectPath).AddDesktop().Resource;
		return (builder.Build(), head);
	}

	private static Task StartAsync(DistributedApplication app, UnoHeadResource head) =>
		app.Services.GetRequiredService<IDistributedApplicationEventing>().PublishAsync(new BeforeResourceStartedEvent(head, app.Services));

	private static string? State(DistributedApplication app, UnoHeadResource head) =>
		app.Services.GetRequiredService<ResourceNotificationService>().TryGetCurrentState(head.Name, out var current)
			? current.Snapshot.State?.Text
			: null;

	[TestMethod]
	public async Task When_HeadStarts_Then_ItIsBuiltAndTheLockIsFree()
	{
		var (app, head) = CreateApp();
		await using var _ = app;

		await StartAsync(app, head);

		State(app, head).Should().Be("Building");
		head.Project.BuildLock.CurrentCount.Should().Be(1);
	}

	[TestMethod]
	public async Task When_AnotherHeadOfTheAppBuilds_Then_ItIsQueuedUntilThatBuildEnds()
	{
		var (app, head) = CreateApp();
		await using var _ = app;
		await head.Project.BuildLock.WaitAsync();

		var start = StartAsync(app, head);
		using (var timeout = new CancellationTokenSource(WaitTimeout))
		{
			await app.Services.GetRequiredService<ResourceNotificationService>().WaitForResourceAsync(head.Name, "Queued", timeout.Token);
		}

		start.IsCompleted.Should().BeFalse();
		head.Project.BuildLock.Release();
		await start.WaitAsync(WaitTimeout);
		State(app, head).Should().Be("Building");
	}

	[TestMethod]
	public async Task When_BuildFails_Then_TheStartFailsAndTheLockIsFree()
	{
		var (app, head) = CreateApp();
		await using var _ = app;
		File.WriteAllText(Path.Join(_solution!.AppFolder, "fail.txt"), string.Empty);

		var start = () => StartAsync(app, head);

		await start.Should().ThrowAsync<DistributedApplicationException>().WithMessage("*'app-desktop' failed*");
		head.Project.BuildLock.CurrentCount.Should().Be(1);
	}

	[TestMethod]
	public async Task When_HeadIsGivenAProperty_Then_TheBuildGetsItToo()
	{
		const string RecordsTheProperty = """
			<Target Name="Build">
				<WriteLinesToFile File="marker.txt" Lines="$(Marker)" Overwrite="true" />
			</Target>
			<Target Name="Restore" />
			""";
		_solution = new TestSolution(RecordsTheProperty);
		var builder = _solution.CreateBuilder();
		builder.Configuration["DcpPublisher:CliPath"] = "dcp";
		builder.Configuration["DcpPublisher:DashboardPath"] = "dashboard";
		var head = builder.AddUnoProject("app", _solution.ProjectPath).AddDesktop().WithArgs("-p:Marker=forwarded").Resource;
		await using var app = builder.Build();

		await StartAsync(app, head);

		File.ReadAllText(Path.Join(_solution.AppFolder, "marker.txt")).Trim().Should().Be("forwarded");
	}

	[TestMethod]
	[DataRow(new[] { "run", "-p:A=1", "--no-build" }, new[] { "-p:A=1" })]
	[DataRow(new[] { "--property:A=1", "/p:B=2" }, new[] { "--property:A=1", "/p:B=2" })]
	[DataRow(new[] { "-c", "Release", "--launch-profile", "Desktop" }, new[] { "-c", "Release" })]
	[DataRow(new[] { "-p", "A=1", "--property", "B=2" }, new[] { "-p", "A=1", "--property", "B=2" })]
	[DataRow(new[] { "--configuration=Release", "-c:Debug", "-p=A=1" }, new[] { "--configuration=Release", "-c:Debug", "-p=A=1" })]
	[DataRow(new[] { "--project", "-path/app.csproj", "-cx" }, new string[0])]
	[DataRow(new[] { "-r", "win-x64", "--os=linux" }, new[] { "-r", "win-x64", "--os=linux" })]
	[DataRow(new[] { "-p:A=1", "--", "-c", "Release" }, new[] { "-p:A=1" })]
	[DataRow(new[] { "--configuration" }, new string[0])]
	public void When_RunArgumentsAreGiven_Then_OnlyPropertiesAndConfigurationReachTheBuild(string[] run, string[] expected) =>
		HeadBuild.SelectBuildArguments(run).Should().Equal(expected);

	[TestMethod]
	public async Task When_TheBuildRestoresWithoutTheClientPackage_Then_TheStartWarns()
	{
		// The restore before the start still had the package; the head's own build restores it away.
		const string RestoreDropsIt = """
			<Target Name="Build" />
			<Target Name="Restore">
				<WriteLinesToFile File="obj/project.assets.json" Lines="{ &quot;version&quot;: 3, &quot;libraries&quot;: { &quot;Other.Package/1.0.0&quot;: { &quot;type&quot;: &quot;package&quot; } } }" Overwrite="true" />
			</Target>
			""";
		_solution = new TestSolution(RestoreDropsIt);
		Directory.CreateDirectory(Path.Join(_solution.AppFolder, "obj"));
		File.WriteAllText(
			Path.Join(_solution.AppFolder, "obj", "project.assets.json"),
			"""{ "version": 3, "libraries": { "Uno.Extensions.Aspire/1.0.0": { "type": "package" } } }""");
		var builder = _solution.CreateBuilder();
		builder.Configuration["DcpPublisher:CliPath"] = "dcp";
		builder.Configuration["DcpPublisher:DashboardPath"] = "dashboard";
		var head = _solution.AddWebAssembly(builder).Resource;
		await using var app = builder.Build();
		var warned = WatchForLog(app, head, "does not reference Uno.Extensions.Aspire");

		await StartAsync(app, head);

		(await warned).Should().BeTrue("the check reads the restore the head's build made");
	}

	private static async Task<bool> WatchForLog(DistributedApplication app, UnoHeadResource head, string text)
	{
		using var timeout = new CancellationTokenSource(WaitTimeout);
		try
		{
			await foreach (var batch in app.Services.GetRequiredService<ResourceLoggerService>().WatchAsync(head.Name).WithCancellation(timeout.Token))
			{
				if (batch.Any(line => line.Content.Contains(text, StringComparison.Ordinal)))
				{
					return true;
				}
			}
		}
		catch (OperationCanceledException)
		{
		}

		return false;
	}

	[TestMethod]
	public async Task When_ProjectDoesNotTargetTheFramework_Then_TheStartFailsBeforeBuilding()
	{
		var (app, head) = CreateApp("<PropertyGroup><TargetFrameworks>net9.0-android</TargetFrameworks></PropertyGroup>" + TestSolution.BuildTargets);
		await using var _ = app;

		var start = () => StartAsync(app, head);

		await start.Should().ThrowAsync<DistributedApplicationException>().WithMessage("*does not target (net9.0-android)*WithTargetFramework*");
		State(app, head).Should().NotBe("Building");
	}
}
