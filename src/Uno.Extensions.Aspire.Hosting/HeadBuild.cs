using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

	// A first build restores packages and can take minutes; a build stuck longer than this is reported as failed.
	private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

	internal static async Task RunAsync(UnoHeadResource head, ResourceNotificationService notifications, ILogger logger, CancellationToken cancellationToken)
	{
		EnsureTheProjectTargetsIt(head);

		var buildLock = head.App.BuildLock;
		if (!await buildLock.WaitAsync(TimeSpan.Zero, cancellationToken).ConfigureAwait(false))
		{
			logger.LogInformation("Waiting for another head of '{App}' to finish building.", head.App.Name);
			await PublishStateAsync(notifications, head, QueuedState).ConfigureAwait(false);
			await buildLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		try
		{
			await PublishStateAsync(notifications, head, BuildingState).ConfigureAwait(false);
			await BuildAsync(head, logger, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			buildLock.Release();
		}
	}

	private static void EnsureTheProjectTargetsIt(UnoHeadResource head)
	{
		var frameworks = head.App.TargetFrameworks;
		if (frameworks.Count > 0 && !frameworks.Contains(head.TargetFramework, StringComparer.OrdinalIgnoreCase))
		{
			throw new DistributedApplicationException(
				$"'{head.Name}' runs {head.TargetFramework}, which the project does not target ({string.Join(", ", frameworks)}). Set another one with WithTargetFramework.");
		}
	}

	private static Task PublishStateAsync(ResourceNotificationService notifications, UnoHeadResource head, string state) =>
		notifications.PublishUpdateAsync(head, snapshot => snapshot with { State = new ResourceStateSnapshot(state, KnownResourceStateStyles.Info) });

	private static async Task BuildAsync(UnoHeadResource head, ILogger logger, CancellationToken cancellationToken)
	{
		var start = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = head.App.ProjectDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};
		foreach (var argument in new[] { "build", head.App.ProjectPath, "-f", head.TargetFramework })
		{
			start.ArgumentList.Add(argument);
		}

		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeout.CancelAfter(Timeout);

		using var process = Process.Start(start) ?? throw new DistributedApplicationException($"'{head.Name}' could not start dotnet build.");
		var output = ForwardAsync(process.StandardOutput, line => logger.LogInformation("{Line}", line));
		var errors = ForwardAsync(process.StandardError, line => logger.LogWarning("{Line}", line));
		try
		{
			await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			process.Kill(entireProcessTree: true);
			if (cancellationToken.IsCancellationRequested)
			{
				throw;
			}

			throw new DistributedApplicationException($"The build of '{head.Name}' did not finish in {Timeout.TotalMinutes} minutes.");
		}

		await Task.WhenAll(output, errors).ConfigureAwait(false);
		if (process.ExitCode != 0)
		{
			throw new DistributedApplicationException($"The build of '{head.Name}' failed. Its errors are in the head's log.");
		}
	}

	private static async Task ForwardAsync(StreamReader reader, Action<string> log)
	{
		while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
		{
			log(line);
		}
	}
}
