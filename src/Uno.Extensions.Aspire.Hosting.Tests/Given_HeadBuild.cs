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
		var head = builder.AddUnoApp("app", _solution.ProjectPath).AddDesktop().Resource;
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
		head.App.BuildLock.CurrentCount.Should().Be(1);
	}

	[TestMethod]
	public async Task When_AnotherHeadOfTheAppBuilds_Then_ItIsQueuedUntilThatBuildEnds()
	{
		var (app, head) = CreateApp();
		await using var _ = app;
		await head.App.BuildLock.WaitAsync();

		var start = StartAsync(app, head);
		using (var timeout = new CancellationTokenSource(WaitTimeout))
		{
			await app.Services.GetRequiredService<ResourceNotificationService>().WaitForResourceAsync(head.Name, "Queued", timeout.Token);
		}

		start.IsCompleted.Should().BeFalse();
		head.App.BuildLock.Release();
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
		head.App.BuildLock.CurrentCount.Should().Be(1);
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
