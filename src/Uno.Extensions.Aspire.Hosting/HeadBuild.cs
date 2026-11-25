using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Builds a head before it starts, one head of an app at a time: heads share the project's obj folder, and two builds
/// of it at once fail. The head then runs with <c>dotnet run --no-build</c>.
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
internal static class HeadBuild
{
	internal const string QueuedState = "Queued";
	internal const string BuildingState = "Building";
	internal const string WaitingForDevServerState = "Waiting for dev server";

	// A first build restores packages and can take minutes; a build stuck longer than this is reported as failed.
	private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

	internal static async Task RunAsync(
		UnoHeadResource head,
		ResourceNotificationService notifications,
		DistributedApplicationExecutionContext executionContext,
		ILogger logger,
		CancellationToken cancellationToken)
	{
		EnsureTheProjectTargetsIt(head);
		var forwarded = WithDevServerPort(
			await ArgumentsForTheBuildAsync(head, executionContext, logger, cancellationToken).ConfigureAwait(false),
			await DevServerPortAsync(head, notifications, logger, cancellationToken).ConfigureAwait(false));

		var buildLock = head.Project.BuildLock;
		if (!await buildLock.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
		{
			logger.LogInformation("Waiting for another head of '{App}' to finish building.", head.Project.Name);
			await PublishStateAsync(notifications, head, QueuedState).ConfigureAwait(false);
			await buildLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		try
		{
			await PublishStateAsync(notifications, head, BuildingState).ConfigureAwait(false);
			await BuildAsync(head, forwarded, logger, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			buildLock.Release();
		}
	}

	private static void EnsureTheProjectTargetsIt(UnoHeadResource head)
	{
		var frameworks = head.Project.TargetFrameworks;
		if (frameworks.Count > 0 && !frameworks.Contains(head.TargetFramework, StringComparer.OrdinalIgnoreCase))
		{
			throw new DistributedApplicationException(
				$"'{head.Name}' runs {head.TargetFramework}, which the project does not target ({string.Join(", ", frameworks)}). Set another one with WithTargetFramework.");
		}
	}

	// The head runs with --no-build, so the properties and configuration it is given have to reach the build too.
	private static async Task<IReadOnlyList<string>> ArgumentsForTheBuildAsync(
		UnoHeadResource head,
		DistributedApplicationExecutionContext executionContext,
		ILogger logger,
		CancellationToken cancellationToken)
	{
		var configuration = await ExecutionConfigurationBuilder.Create(head)
			.WithArgumentsConfig()
			.BuildAsync(executionContext, logger, cancellationToken)
			.ConfigureAwait(false);

		return SelectBuildArguments(configuration.Arguments.Select(argument => argument.Value).ToList());
	}

	// A head built outside an AppHost run, by a test for example, does not wait.
	private static async Task<int?> DevServerPortAsync(UnoHeadResource head, ResourceNotificationService notifications, ILogger logger, CancellationToken cancellationToken)
	{
		if (head.Project.DevServer is not { IsStarting: true } devServer)
		{
			return null;
		}

		if (!devServer.Port.IsCompleted)
		{
			logger.LogInformation("Waiting for the dev server '{DevServer}'.", devServer.Name);
			await PublishStateAsync(notifications, head, WaitingForDevServerState).ConfigureAwait(false);
		}

		try
		{
			return await devServer.Port.WaitAsync(UnoDevServer.HeadWait, cancellationToken).ConfigureAwait(false);
		}
		catch (TimeoutException)
		{
			logger.LogWarning("The dev server did not start in time: '{Head}' is built without Hot Reload.", head.Name);
			return null;
		}
	}

	// First, so a port given with WithArgs wins.
	internal static IReadOnlyList<string> WithDevServerPort(IReadOnlyList<string> arguments, int? port) =>
		port is null
			? arguments
			: [$"-p:{UnoDevServer.PortProperty}={port.Value.ToString(CultureInfo.InvariantCulture)}", .. arguments];

	// The options of dotnet build that change what it produces or where, in their joined (-p:A=1, -c:Release) and
	// separate (-p A=1, -c Release) forms.
	private static readonly string[] BuildOptions =
		["-p", "/p", "--property", "-property", "-c", "--configuration", "-r", "--runtime", "-a", "--arch", "--os"];

	internal static IReadOnlyList<string> SelectBuildArguments(IReadOnlyList<string> runArguments)
	{
		var selected = new List<string>();
		for (var index = 0; index < runArguments.Count; index++)
		{
			var argument = runArguments[index];
			if (argument == "--")
			{
				// What follows is for the app.
				break;
			}

			if (IsJoinedBuildOption(argument))
			{
				selected.Add(argument);
			}
			else if (BuildOptions.Contains(argument, StringComparer.OrdinalIgnoreCase) && index + 1 < runArguments.Count)
			{
				selected.Add(argument);
				selected.Add(runArguments[++index]);
			}
		}

		return selected;
	}

	private static bool IsJoinedBuildOption(string argument) =>
		BuildOptions.Any(option =>
			argument.StartsWith(option + ":", StringComparison.OrdinalIgnoreCase)
			|| argument.StartsWith(option + "=", StringComparison.OrdinalIgnoreCase));

	private static Task PublishStateAsync(ResourceNotificationService notifications, UnoHeadResource head, string state) =>
		notifications.PublishUpdateAsync(head, snapshot => snapshot with { State = new ResourceStateSnapshot(state, KnownResourceStateStyles.Info) });

	private static async Task BuildAsync(UnoHeadResource head, IReadOnlyList<string> forwarded, ILogger logger, CancellationToken cancellationToken)
	{
		var exitCode = await DotnetCommand.RunAsync(
			["build", head.Project.ProjectPath, "-f", head.TargetFramework, .. forwarded],
			head.Project.ProjectDirectory,
			Timeout,
			line => logger.LogInformation("{Line}", line),
			line => logger.LogWarning("{Line}", line),
			cancellationToken).ConfigureAwait(false);

		if (exitCode is null)
		{
			throw new DistributedApplicationException($"The build of '{head.Name}' did not finish in {Timeout.TotalMinutes} minutes.");
		}

		if (exitCode != 0)
		{
			throw new DistributedApplicationException($"The build of '{head.Name}' failed. Its errors are in the head's log.");
		}
	}
}
