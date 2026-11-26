using System.Collections.Concurrent;
using System.Diagnostics;
using Aspire.Hosting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Aspire.Hosting.Tests;

[TestClass]
public class Given_DotnetCommand
{
	// An app that starts a child writing to the app's own output for far longer than the command may take, then exits.
	private const string App = """
		using System.Diagnostics;

		var writer = OperatingSystem.IsWindows()
			? new ProcessStartInfo("ping", "-n 120 127.0.0.1")
			: new ProcessStartInfo("sh", ["-c", "i=0; while [ $i -lt 120 ]; do echo tick; sleep 1; i=$((i+1)); done"]);
		writer.UseShellExecute = false;
		var child = Process.Start(writer)!;
		File.WriteAllText("child.pid", child.Id.ToString());
		Console.WriteLine("started");
		""";

	private const string SlowApp = """
		Thread.Sleep(TimeSpan.FromSeconds(60));
		""";

	private static readonly TimeSpan ChildLineWait = TimeSpan.FromSeconds(10);

	private string _folder = "";

	[TestInitialize]
	public void CreateApp()
	{
		_folder = Path.Join(Path.GetTempPath(), $"dotnet-command-{Guid.NewGuid():N}");
		Directory.CreateDirectory(_folder);
		File.WriteAllText(Path.Join(_folder, "app.cs"), App);
	}

	[TestCleanup]
	public void StopChild()
	{
		var pidFile = Path.Join(_folder, "child.pid");
		if (File.Exists(pidFile) && int.TryParse(File.ReadAllText(pidFile), out var pid))
		{
			try
			{
				Process.GetProcessById(pid).Kill();
			}
			catch (ArgumentException)
			{
				// Already exited.
			}
		}

		Directory.Delete(_folder, recursive: true);
	}

	[TestMethod]
	[Timeout(180_000)]
	public async Task When_AChildKeepsTheOutputOpen_Then_TheCommandEndsWithItsProcessAndTheChildsLinesKeepComing()
	{
		var lines = new ConcurrentQueue<string>();
		var elapsed = Stopwatch.StartNew();

		var exitCode = await DotnetCommand.RunAsync(["run", "app.cs"], _folder, TimeSpan.FromMinutes(2), lines.Enqueue, _ => { }, CancellationToken.None);

		exitCode.Should().Be(0);
		lines.Should().Contain("started");
		elapsed.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(100));

		var countAtReturn = lines.Count;
		var waited = Stopwatch.StartNew();
		while (lines.Count == countAtReturn && waited.Elapsed < ChildLineWait)
		{
			await Task.Delay(TimeSpan.FromMilliseconds(200));
		}

		lines.Count.Should().BeGreaterThan(countAtReturn, "the child's lines are still forwarded");
	}

	[TestMethod]
	[Timeout(120_000)]
	public async Task When_TheCommandTakesLongerThanItsTimeout_Then_ItIsStoppedAndNoExitCodeIsReturned()
	{
		File.WriteAllText(Path.Join(_folder, "slow.cs"), SlowApp);
		var elapsed = Stopwatch.StartNew();

		var exitCode = await DotnetCommand.RunAsync(["run", "slow.cs"], _folder, TimeSpan.FromSeconds(3), _ => { }, _ => { }, CancellationToken.None);

		exitCode.Should().BeNull();
		elapsed.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30));
	}
}
