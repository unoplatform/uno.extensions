using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Gives the heads of each solution a dev server for Hot Reload through the <c>uno.devserver</c> CLI: the one already
/// running for the solution, an IDE's for example, or one the CLI starts for the AppHost. See
/// specs/027-aspire-support/spec.md, "Hot Reload".
/// </summary>
[Experimental(UnoAppDiagnostics.ExperimentalId)]
internal static partial class UnoDevServer
{
	internal const string ResourceName = "uno-devserver";

	/// <summary>
	/// The AppHost setting that turns the dev server off when <c>false</c>, for example <c>Uno__DevServer__Enabled=false</c>.
	/// </summary>
	internal const string EnabledSetting = "Uno:DevServer:Enabled";

	// The build property the Uno.Sdk turns into the address the app connects to, as the IDE sets it.
	internal const string PortProperty = "UnoRemoteControlPort";

	// The Uno.Sdk adds it to Debug builds; the CLI is released with the same version.
	internal const string DevServerPackageId = "Uno.WinUI.DevServer";

	private const string ToolPackageId = "uno.devserver";
	private const string ResourceType = "DevServer";
	private const string UnavailableState = "Unavailable";
	private const string TurnOffHint = "Set Uno__DevServer__Enabled=false in the AppHost's environment to run without it.";

	// A first run of the CLI downloads it.
	private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(2);

	// A first restore downloads the app's packages, the dev server among them.
	private static readonly TimeSpan RestoreTimeout = TimeSpan.FromMinutes(10);

	// From the CLI's start to the dev server's registration, a first download of the CLI included.
	private static readonly TimeSpan RegistrationTimeout = TimeSpan.FromMinutes(2);
	private static readonly TimeSpan RegistrationPollInterval = TimeSpan.FromSeconds(1);

	// A process with the dev server's id and a start time further away than this is another process.
	private static readonly TimeSpan StartTimeTolerance = TimeSpan.FromSeconds(2);

	// Longer than a first restore and registration together: a head built without the port has no Hot Reload.
	internal static readonly TimeSpan HeadWait = TimeSpan.FromMinutes(15);

	/// <summary>
	/// Adds the dev server of the project's solution, once per solution, unless the AppHost turned it off.
	/// </summary>
	internal static UnoDevServerResource? GetOrAdd(IDistributedApplicationBuilder builder, UnoProjectResource project)
	{
		if (!IsEnabled(builder.Configuration[EnabledSetting]))
		{
			return null;
		}

		var solutionDirectory = FindSolutionDirectory(project.ProjectDirectory);
		var devServers = builder.Resources.OfType<UnoDevServerResource>().ToList();
		if (devServers.FirstOrDefault(devServer => SamePath(devServer.Folder, solutionDirectory ?? project.ProjectDirectory)) is { } existing)
		{
			return existing;
		}

		var name = devServers.Count == 0 ? ResourceName : $"{ResourceName}-{devServers.Count + 1}";
		if (builder.Resources.Any(resource => string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase)))
		{
			throw new DistributedApplicationException(
				$"The AppHost names the dev server of '{project.Name}' '{name}', which another resource uses. Rename that resource, or turn the dev server off with the setting {EnabledSetting}=false.");
		}

		var added = new UnoDevServerResource(name, project, solutionDirectory);
		builder.AddResource(added)
			.WithInitialState(new CustomResourceSnapshot
			{
				ResourceType = ResourceType,
				State = KnownResourceStates.Starting,
				Properties = [],
			})
			.ExcludeFromManifest();

		// Before any resource starts, so every head of the run waits for the port.
		builder.Eventing.Subscribe<BeforeStartEvent>((@event, startCancellation) =>
		{
			added.MarkStarting();
			var stopping = @event.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
			_ = StartAsync(added, @event.Services, stopping);
			return Task.CompletedTask;
		});

		return added;
	}

	internal static bool IsEnabled(string? setting)
	{
		if (setting is null)
		{
			return true;
		}

		return bool.TryParse(setting, out var enabled)
			? enabled
			: throw new DistributedApplicationException($"The setting {EnabledSetting} is '{setting}': set it to true or false.");
	}

	/// <summary>
	/// The folder of the solution that holds the project, or <c>null</c> when no parent has one: the CLI looks for a
	/// solution in its working folder only, and a dev server serves a solution.
	/// </summary>
	internal static string? FindSolutionDirectory(string projectDirectory)
	{
		for (var directory = new DirectoryInfo(projectDirectory); directory is not null; directory = directory.Parent)
		{
			if (directory.Exists
				&& (directory.EnumerateFiles("*.sln").Any() || directory.EnumerateFiles("*.slnx").Any()))
			{
				return directory.FullName;
			}
		}

		return null;
	}

	internal static IReadOnlyList<string> ToolArguments(string version, params string[] command) =>
		["dnx", "-y", $"{ToolPackageId}@{version}", .. command];

	/// <summary>
	/// The JSON document of <c>disco --json</c>, which starts a line: dnx can write lines before it when it downloads the
	/// tool. <c>null</c> when there is none or it is malformed.
	/// </summary>
	internal static DevServerDiscovery? ParseDiscovery(string output)
	{
		var start = ("\n" + output).IndexOf("\n{", StringComparison.Ordinal);
		var end = output.LastIndexOf('}');
		if (start < 0 || end < start)
		{
			return null;
		}

		try
		{
			return JsonSerializer.Deserialize(output.AsSpan(start, end - start + 1), DevServerJsonContext.Default.DevServerDiscovery);
		}
		catch (JsonException)
		{
			return null;
		}
	}

	internal static DevServerActiveServer? FindRunning(DevServerDiscovery discovery)
	{
		if (discovery.SelectedSolutionPath is not { Length: > 0 } solution || discovery.ActiveServers is not { } servers)
		{
			return null;
		}

		return servers.FirstOrDefault(server =>
			server is { ProcessId: not null, Port: not null, SolutionPath.Length: > 0 }
			&& SamePath(server.SolutionPath, solution));
	}

	/// <summary>
	/// Whether a version read from the app's assets file is a package version, before it is passed to dnx.
	/// </summary>
	internal static bool IsPackageVersion(string version) => PackageVersionPattern().IsMatch(version);

	[GeneratedRegex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z][0-9A-Za-z.-]*)?$")]
	private static partial Regex PackageVersionPattern();

	private static bool SamePath(string first, string second) =>
		string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), StringComparison.OrdinalIgnoreCase);

	// Fire and forget from BeforeStartEvent: the finally sets the port, so no head waits longer than this method.
	private static async Task StartAsync(UnoDevServerResource devServer, IServiceProvider services, CancellationToken stopping)
	{
		ILogger? logger = null;
		ResourceNotificationService? notifications = null;
		try
		{
			// Off the AppHost's start: the steps below read files and run processes.
			await Task.Yield();
			logger = services.GetRequiredService<ResourceLoggerService>().GetLogger(devServer);
			notifications = services.GetRequiredService<ResourceNotificationService>();

			if (devServer.SolutionDirectory is not { } solutionDirectory)
			{
				await PublishUnavailableAsync(notifications, devServer, logger, $"No solution was found in the folders above {devServer.Project.ProjectPath}: a dev server serves a solution.").ConfigureAwait(false);
				return;
			}

			var (version, problem) = await DevServerVersionAsync(devServer.Project, logger, stopping).ConfigureAwait(false);
			if (version is null)
			{
				await PublishUnavailableAsync(notifications, devServer, logger, problem!).ConfigureAwait(false);
				return;
			}

			var (running, registrationProblem) = await StartOrReuseAsync(version, solutionDirectory, logger, stopping).ConfigureAwait(false);
			if (running is not { ProcessId: { } processId, Port: { } port })
			{
				await PublishUnavailableAsync(notifications, devServer, logger, registrationProblem!).ConfigureAwait(false);
				return;
			}

			devServer.SetPort(port);
			var startedByAppHost = running.ParentProcessId == Environment.ProcessId;
			logger.LogInformation("Using the dev server of {Solution}: process {ProcessId}, port {Port}.", running.SolutionPath, processId, port);
			await PublishSafelyAsync(notifications, devServer, logger, snapshot => snapshot with
			{
				State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success),
				Properties = snapshot.Properties
					.Add(new ResourcePropertySnapshot("Port", port))
					.Add(new ResourcePropertySnapshot("Process", processId))
					.Add(new ResourcePropertySnapshot("Started by", startedByAppHost ? "AppHost" : $"Process {running.ParentProcessId?.ToString(CultureInfo.InvariantCulture)}")),
			}).ConfigureAwait(false);

			await WatchAsync(devServer, running, notifications, logger, stopping).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (stopping.IsCancellationRequested)
		{
			// The AppHost is stopping.
		}
		catch (Exception exception) when (logger is not null && notifications is not null)
		{
			await PublishFailureAsync(devServer, exception, notifications, logger).ConfigureAwait(false);
		}
		finally
		{
			devServer.SetPort(null);
		}
	}

	// The CLI matching the app's dev server package: another version may not know its options or its host. The restore
	// is a no-op once done, and keeps the version in step with the Uno.Sdk the app uses now.
	private static async Task<(string? Version, string? Problem)> DevServerVersionAsync(UnoProjectResource project, ILogger logger, CancellationToken cancellationToken)
	{
		var output = new List<string>();

		// A head's build uses the same obj folder.
		await project.BuildLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		int? exitCode;
		try
		{
			exitCode = await DotnetCommand.RunAsync(
				["msbuild", project.ProjectPath, "-restore", "-getProperty:ProjectAssetsFile", "-nologo"],
				project.ProjectDirectory,
				RestoreTimeout,
				output.Add,
				line => logger.LogWarning("{Line}", line),
				cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			project.BuildLock.Release();
		}

		if (exitCode is null)
		{
			return (null, $"Restoring {project.Name} did not finish in {RestoreTimeout.TotalMinutes} minutes. {TurnOffHint}");
		}

		if (exitCode != 0)
		{
			foreach (var line in output)
			{
				logger.LogWarning("{Line}", line);
			}

			return (null, $"Restoring {project.Name} failed, offline or with a package feed out of reach: its output is in this log. {TurnOffHint}");
		}

		// The property's value is the last line: the restore can write before it.
		var assetsPath = output.LastOrDefault(line => line.Trim().Length > 0)?.Trim() ?? "";
		return ProjectAssets.Version(assetsPath, DevServerPackageId) switch
		{
			null => (null, $"{project.Name} does not reference {DevServerPackageId}, which the Uno.Sdk adds to Debug builds."),
			var version when !IsPackageVersion(version) => (null, $"{project.Name} restored {DevServerPackageId} '{version}', which is not a package version."),
			var version => (version, null),
		};
	}

	// The CLI reuses the dev server running for the solution, replacing one whose parent is gone, or starts one that stops
	// with the AppHost. It returns once the dev server is ready, or stays to relay its output: either way the AppHost waits
	// for the registration, not for the CLI.
	private static async Task<(DevServerActiveServer? Running, string? Problem)> StartOrReuseAsync(string version, string solutionDirectory, ILogger logger, CancellationToken stopping)
	{
		var parentProcessId = Environment.ProcessId.ToString(CultureInfo.InvariantCulture);
		var (cli, forwarding) = DotnetCommand.Start(
			ToolArguments(version, "start", "--ppid", parentProcessId),
			solutionDirectory,
			line => logger.LogInformation("{Line}", line),
			line => logger.LogWarning("{Line}", line));

		// The CLI only: the dev server stops on its own once the AppHost is gone, and removes its registration.
		using var stopTheCli = stopping.Register(() => DotnetCommand.StopProcess(cli));
		_ = DotnetCommand.DisposeWhenTheOutputEndsAsync(cli, forwarding);

		var elapsed = Stopwatch.StartNew();
		var recognized = true;
		while (elapsed.Elapsed < RegistrationTimeout)
		{
			var output = new StringBuilder();
			if (await DotnetCommand.RunAsync(ToolArguments(version, "disco", "--json"), solutionDirectory, CommandTimeout, line => output.AppendLine(line), _ => { }, stopping).ConfigureAwait(false) is 0)
			{
				var discovery = ParseDiscovery(output.ToString());
				recognized = discovery is { SelectedSolutionPath.Length: > 0 };
				if (discovery is not null && FindRunning(discovery) is { } running)
				{
					return (running, null);
				}
			}

			if (HasFailed(cli))
			{
				return (null, $"The uno.devserver {version} CLI could not start the dev server: its output is in this log. {TurnOffHint}");
			}

			await Task.Delay(RegistrationPollInterval, stopping).ConfigureAwait(false);
		}

		return recognized
			? (null, $"No dev server registered for the solution within {RegistrationTimeout.TotalMinutes} minutes: the output of the uno.devserver {version} CLI is in this log. {TurnOffHint}")
			: (null, $"The output of the uno.devserver {version} CLI was not recognized: this version may be too old for the AppHost. {TurnOffHint}");
	}

	private static bool HasFailed(Process cli)
	{
		try
		{
			return cli.HasExited && cli.ExitCode != 0;
		}
		catch (InvalidOperationException)
		{
			// Disposed once its output ended, after it exited.
			return false;
		}
	}

	// Reports a dev server that stops during the run: the heads keep a port nothing listens on.
	private static async Task WatchAsync(UnoDevServerResource devServer, DevServerActiveServer running, ResourceNotificationService notifications, ILogger logger, CancellationToken stopping)
	{
		if (TryGetProcess(running) is { } process)
		{
			using (process)
			{
				await process.WaitForExitAsync(stopping).ConfigureAwait(false);
			}
		}

		logger.LogWarning("The dev server (process {ProcessId}) stopped. Hot Reload is off until the AppHost restarts.", running.ProcessId);
		await PublishSafelyAsync(notifications, devServer, logger, snapshot => snapshot with
		{
			State = new ResourceStateSnapshot(KnownResourceStates.Exited, KnownResourceStateStyles.Error),
		}).ConfigureAwait(false);
	}

	private static Process? TryGetProcess(DevServerActiveServer running)
	{
		Process process;
		try
		{
			process = Process.GetProcessById(running.ProcessId!.Value);
		}
		catch (ArgumentException)
		{
			// Already exited.
			return null;
		}

		try
		{
			// The id of a process that exited can be given to another one.
			if (running.StartTime is { } startTime
				&& (process.StartTime.ToUniversalTime() - startTime.UtcDateTime).Duration() > StartTimeTolerance)
			{
				process.Dispose();
				return null;
			}
		}
		catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
		{
			// Its start time cannot be read: it is watched as the dev server.
		}

		return process;
	}

	private static async Task PublishFailureAsync(UnoDevServerResource devServer, Exception exception, ResourceNotificationService notifications, ILogger logger)
	{
		// After the port was given to the heads, the dev server works: only the watch failed.
		if (devServer.Port is { IsCompletedSuccessfully: true, Result: not null })
		{
			logger.LogWarning(exception, "The dev server is no longer watched: the dashboard keeps its last state.");
			return;
		}

		logger.LogWarning(exception, "The dev server could not start. Hot Reload is off for this run.");
		await PublishSafelyAsync(notifications, devServer, logger, snapshot => snapshot with
		{
			State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error),
			Properties = snapshot.Properties.Add(new ResourcePropertySnapshot("Reason", $"{exception.Message} {TurnOffHint}")),
		}).ConfigureAwait(false);
	}

	// The state first, so the dashboard says why before the heads build without the port.
	private static async Task PublishUnavailableAsync(ResourceNotificationService notifications, UnoDevServerResource devServer, ILogger logger, string reason)
	{
		logger.LogWarning("{Reason} Hot Reload is off for this run.", reason);
		await PublishSafelyAsync(notifications, devServer, logger, snapshot => snapshot with
		{
			State = new ResourceStateSnapshot(UnavailableState, KnownResourceStateStyles.Warn),
			Properties = snapshot.Properties.Add(new ResourcePropertySnapshot("Reason", reason)),
		}).ConfigureAwait(false);
		devServer.SetPort(null);
	}

	// Failures here must not turn a working dev server into a failed one: the port is already set.
	private static async Task PublishSafelyAsync(ResourceNotificationService notifications, UnoDevServerResource devServer, ILogger logger, Func<CustomResourceSnapshot, CustomResourceSnapshot> update)
	{
		try
		{
			await notifications.PublishUpdateAsync(devServer, update).ConfigureAwait(false);
		}
		catch (Exception exception)
		{
			logger.LogDebug(exception, "The state of the dev server could not be published.");
		}
	}
}
