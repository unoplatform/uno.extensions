using System.ComponentModel;
using System.Diagnostics;

namespace Aspire.Hosting;

/// <summary>
/// Runs <c>dotnet</c>, forwarding its output line by line.
/// </summary>
internal static class DotnetCommand
{
	// After the exit, the time left to read the last lines of output.
	private static readonly TimeSpan OutputDrainTimeout = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Runs <c>dotnet</c> and returns its exit code, or <c>null</c> when it did not finish in time. The process tree is
	/// stopped on a timeout and on a cancellation.
	/// </summary>
	internal static async Task<int?> RunAsync(
		IEnumerable<string> arguments,
		string workingDirectory,
		TimeSpan timeout,
		Action<string> onOutput,
		Action<string> onError,
		CancellationToken cancellationToken)
	{
		using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeoutSource.CancelAfter(timeout);

		var (process, forwarding) = Start(arguments, workingDirectory, onOutput, onError);
		try
		{
			await process.WaitForExitAsync(timeoutSource.Token).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			StopTree(process);
			_ = DisposeWhenTheOutputEndsAsync(process, forwarding);
			if (cancellationToken.IsCancellationRequested)
			{
				throw;
			}

			return null;
		}

		var exitCode = process.ExitCode;
		try
		{
			await forwarding.WaitAsync(OutputDrainTimeout, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception exception) when (exception is TimeoutException or OperationCanceledException)
		{
			// A child that inherited the output still writes to it: its lines keep being forwarded.
		}

		_ = DisposeWhenTheOutputEndsAsync(process, forwarding);
		return exitCode;
	}

	/// <summary>
	/// Starts <c>dotnet</c> without waiting for it. The returned task ends with the output.
	/// </summary>
	internal static (Process Process, Task Forwarding) Start(
		IEnumerable<string> arguments,
		string workingDirectory,
		Action<string> onOutput,
		Action<string> onError)
	{
		var start = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = workingDirectory,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};
		foreach (var argument in arguments)
		{
			start.ArgumentList.Add(argument);
		}

		var process = Process.Start(start) ?? throw new DistributedApplicationException("dotnet could not be started.");
		var forwarding = Task.WhenAll(
			ForwardAsync(process.StandardOutput, onOutput),
			ForwardAsync(process.StandardError, onError));
		return (process, forwarding);
	}

	/// <summary>
	/// Stops the process and the ones it started.
	/// </summary>
	internal static void StopTree(Process process) => Stop(process, entireProcessTree: true);

	/// <summary>
	/// Stops the process only: the ones it started keep running.
	/// </summary>
	internal static void StopProcess(Process process) => Stop(process, entireProcessTree: false);

	// Fire and forget: forwarding never throws.
	internal static async Task DisposeWhenTheOutputEndsAsync(Process process, Task forwarding)
	{
		await forwarding.ConfigureAwait(false);
		process.Dispose();
	}

	private static void Stop(Process process, bool entireProcessTree)
	{
		try
		{
			process.Kill(entireProcessTree);
		}
		catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or AggregateException)
		{
			// Already exited, or not stoppable.
		}
	}

	private static async Task ForwardAsync(StreamReader reader, Action<string> forward)
	{
		try
		{
			while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
			{
				forward(line);
			}
		}
		catch (Exception exception) when (exception is IOException or ObjectDisposedException)
		{
			// The pipe broke: nothing more to forward.
		}
	}
}
