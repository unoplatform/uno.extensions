using System.ComponentModel;
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

	internal static async Task RunAsync(
		UnoHeadResource head,
		ResourceNotificationService notifications,
		DistributedApplicationExecutionContext executionContext,
		ILogger logger,
		CancellationToken cancellationToken)
	{
		EnsureTheProjectTargetsIt(head);
		var forwarded = await ArgumentsForTheBuildAsync(head, executionContext, logger, cancellationToken).ConfigureAwait(false);

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
			await BuildAsync(head, forwarded, logger, cancellationToken).ConfigureAwait(false);
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
		var start = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = head.App.ProjectDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};
		foreach (var argument in new[] { "build", head.App.ProjectPath, "-f", head.TargetFramework }.Concat(forwarded))
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
			StopIfRunning(process);
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

	// The build can exit between the timeout and the kill; that must not replace the timeout or the cancellation.
	private static void StopIfRunning(Process process)
	{
		try
		{
			process.Kill(entireProcessTree: true);
		}
		catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or AggregateException)
		{
			// Exited meanwhile, or could not be stopped: either way the caller reports the timeout or the cancellation.
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
