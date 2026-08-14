using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.RuntimeTests;

/// <summary>
/// Drives the runtime tests automatically on Android and iOS CI and writes NUnit results to a file
/// the host-side script collects.
/// </summary>
/// <remarks>
/// <para>
/// The desktop and WebAssembly heads use the engine's own embedded runner via
/// <c>UNO_RUNTIME_TESTS_RUN_TESTS</c>. That path is unusable on mobile: the engine's
/// <c>RuntimeTestEmbeddedRunner</c> <c>[ModuleInitializer]</c> calls
/// <see cref="Console.CancelKeyPress"/>, which throws <see cref="PlatformNotSupportedException"/>
/// inside a sandboxed iOS app. So this reads <c>UITEST_RUNTIME_*</c> variables instead - a
/// different set of names specifically so that module initializer stays asleep.
/// </para>
/// <para>
/// Ported from the equivalent in Studio Live, which in turn mirrors uno core's Skia iOS / Android
/// runtime-test pipelines. The comments about dispatcher affinity and <c>TryEnqueue</c> failure are
/// carried over deliberately - both were paid for with CI debugging time.
/// </para>
/// </remarks>
internal static class MobileRuntimeTestsAutostart
{
	private const string ResultFileVariable = "UITEST_RUNTIME_AUTOSTART_RESULT_FILE";
	private const string FilterVariable = "UITEST_RUNTIME_TESTS_FILTER";

	/// <summary>
	/// If <c>UITEST_RUNTIME_AUTOSTART_RESULT_FILE</c> is set, waits for the
	/// <see cref="UnitTestsControl"/> to be live, runs the filtered tests, writes the NUnit
	/// document to that path, then exits the process.
	/// </summary>
	/// <remarks>
	/// Fire-and-forget from <c>App.OnLaunched</c> after the window content is set. Any exception
	/// exits with code 1 rather than being swallowed, so a failure shows up as a non-zero exit
	/// instead of the host script waiting out its full timeout on a result file that never appears.
	/// </remarks>
	public static void StartIfRequested(Window window)
	{
		ArgumentNullException.ThrowIfNull(window);

		var resultFile = Environment.GetEnvironmentVariable(ResultFileVariable);

		// Always say which branch was taken. Returning silently here is indistinguishable from
		// "ran and produced nothing" in a device log, which cost a full CI round on iOS: the app
		// launched, sat idle for the 1800s timeout and was killed by the harness, with no clue
		// whether the variable had arrived.
		Console.WriteLine(
			$"MobileRuntimeTestsAutostart: {ResultFileVariable}=" +
			(string.IsNullOrEmpty(resultFile) ? "<not set> - autostart disabled" : resultFile));

		if (string.IsNullOrEmpty(resultFile))
		{
			return;
		}

		_ = Task.Run(async () =>
		{
			try
			{
				await RunAsync(window, resultFile).ConfigureAwait(false);
				Console.WriteLine($"MobileRuntimeTestsAutostart: wrote results to {resultFile}");
				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				try
				{
					Console.Error.WriteLine($"MobileRuntimeTestsAutostart failed: {ex}");
				}
				catch (Exception)
				{
					// Best-effort logging only; the exit code below is the real signal.
				}

				Environment.Exit(1);
			}
		});
	}

	private static async Task RunAsync(Window window, string resultFile)
	{
		// UnitTestsControl.RunTests, NUnitTestResultsDocument, Window.Content and VisualTreeHelper
		// are all XAML-affine, and this runs on a worker thread. Every visual-tree touch below hops
		// to the dispatcher; only file I/O stays here.
		var dispatcher = window.DispatcherQueue
			?? throw new InvalidOperationException("Window has no DispatcherQueue.");

		// Let the app settle before driving the UI, matching uno's SamplesApp.
		await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

		var unitTests = await FindUnitTestsControlAsync(window, dispatcher, TimeSpan.FromSeconds(30))
			.ConfigureAwait(false)
			?? throw new InvalidOperationException(
				"Could not locate a UnitTestsControl within 30s of app start.");

		var results = await RunTestsAsync(dispatcher, unitTests, BuildConfigFromEnvironment()).ConfigureAwait(false);

		Directory.CreateDirectory(Path.GetDirectoryName(resultFile) ?? ".");
		await File.WriteAllTextAsync(resultFile, results, Encoding.UTF8).ConfigureAwait(false);
	}

	private static Task<string> RunTestsAsync(
		DispatcherQueue dispatcher,
		UnitTestsControl unitTests,
		UnitTestEngineConfig config)
	{
		// RunContinuationsAsynchronously keeps the awaiter off the dispatcher thread, which would
		// otherwise stall test-shutdown rendering on a UI-thread continuation.
		var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

		if (!dispatcher.TryEnqueue(async () =>
		{
			try
			{
				await unitTests.RunTests(CancellationToken.None, config).ConfigureAwait(true);
				tcs.TrySetResult(unitTests.NUnitTestResultsDocument);
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		}))
		{
			// TryEnqueue returns false when the dispatcher is shutting down (the Android
			// splash-to-main-window transition can briefly trip this). Without this guard the outer
			// await hangs until the host script's timeout and the real cause stays hidden.
			throw new InvalidOperationException(
				"DispatcherQueue rejected the runtime-tests work item - dispatcher is shutting down.");
		}

		return tcs.Task;
	}

	private static async Task<UnitTestsControl?> FindUnitTestsControlAsync(
		Window window,
		DispatcherQueue dispatcher,
		TimeSpan timeout)
	{
		var deadline = DateTimeOffset.UtcNow + timeout;
		while (DateTimeOffset.UtcNow < deadline)
		{
			if (await FindOnUIThreadAsync(dispatcher, window).ConfigureAwait(false) is { } hit)
			{
				return hit;
			}

			await Task.Delay(200).ConfigureAwait(false);
		}

		return null;
	}

	private static Task<UnitTestsControl?> FindOnUIThreadAsync(DispatcherQueue dispatcher, Window window)
	{
		var tcs = new TaskCompletionSource<UnitTestsControl?>(TaskCreationOptions.RunContinuationsAsynchronously);

		if (!dispatcher.TryEnqueue(() =>
		{
			try
			{
				tcs.TrySetResult(window.Content is DependencyObject root ? FindFirstOfType<UnitTestsControl>(root) : null);
			}
			catch (Exception ex)
			{
				tcs.TrySetException(ex);
			}
		}))
		{
			throw new InvalidOperationException(
				"DispatcherQueue rejected the find work item - dispatcher is shutting down.");
		}

		return tcs.Task;
	}

	private static T? FindFirstOfType<T>(DependencyObject root) where T : class
	{
		if (root is T match)
		{
			return match;
		}

		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			if (FindFirstOfType<T>(VisualTreeHelper.GetChild(root, i)) is { } hit)
			{
				return hit;
			}
		}

		return null;
	}

	private static UnitTestEngineConfig BuildConfigFromEnvironment()
	{
		var rawFilter = Environment.GetEnvironmentVariable(FilterVariable);
		UnitTestFilter? filter = null;

		if (!string.IsNullOrWhiteSpace(rawFilter))
		{
			// Base64 on the host side: adb `am start -e` and simctl don't pass raw pipes or spaces
			// cleanly. '!' stands in for '=' padding for the same reason.
			rawFilter = rawFilter.Replace("!", "=");
			var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(rawFilter));
			var joined = string.Join(
				" | ",
				decoded.Split('|').Select(part => part.Trim()).Where(part => part.Length > 0));

			if (joined.Length > 0)
			{
				filter = new UnitTestFilter { Value = joined };
			}
		}

		return new UnitTestEngineConfig
		{
			Filter = filter,
			Attempts = 1,
		};
	}
}
