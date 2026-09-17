using System;
using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Authentication.MSAL;

[TestClass]
public class Given_MsalCacheWriteWatchdog
{
	private static readonly DateTime PreviousRun = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

	private string _folder = string.Empty;
	private string _cacheFile = string.Empty;

	[TestInitialize]
	public void CreateFolder()
	{
		_folder = Path.Combine(Path.GetTempPath(), "uno-ext-msal-watchdog-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_folder);
		_cacheFile = Path.Combine(_folder, "msal.cache");
	}

	[TestCleanup]
	public void DeleteFolder() => Directory.Delete(_folder, recursive: true);

	private static MsalCacheWriteWatchdog Create() => new(NullLogger.Instance);

	/// <summary>A cache file as a previous run left it, with a write time no test write can share.</summary>
	private void LeaveCacheFileFromPreviousRun()
	{
		File.WriteAllText(_cacheFile, "previous");
		File.SetLastWriteTimeUtc(_cacheFile, PreviousRun);
	}

	[TestMethod]
	public void When_FirstWriteCreatesTheFile_Then_NoResetup()
	{
		var watchdog = Create();
		watchdog.Arm(_cacheFile);

		File.WriteAllText(_cacheFile, "written");
		watchdog.OnCacheAccessed(hasStateChanged: true);

		watchdog.TakeResetupRequest().Should().BeFalse();
	}

	[TestMethod]
	public void When_NothingReachesTheFile_Then_ResetupRequestedOnce()
	{
		var watchdog = Create();
		watchdog.Arm(_cacheFile);

		watchdog.OnCacheAccessed(hasStateChanged: true);

		watchdog.TakeResetupRequest().Should().BeTrue("the store swallowed the write");
		watchdog.TakeResetupRequest().Should().BeFalse("a request is taken once");
	}

	/// <summary>
	/// The case <c>Auto</c> creates: the probe was skipped *because* the file exists, so its
	/// existence after a write says nothing - a store that now rejects writes leaves it as it was.
	/// </summary>
	[TestMethod]
	public void When_PreviousRunsFileIsLeftUntouched_Then_ResetupRequested()
	{
		LeaveCacheFileFromPreviousRun();
		var watchdog = Create();
		watchdog.Arm(_cacheFile);

		watchdog.OnCacheAccessed(hasStateChanged: true);

		watchdog.TakeResetupRequest().Should().BeTrue("an untouched file is not a write");
	}

	[TestMethod]
	public void When_PreviousRunsFileIsRewritten_Then_NoResetup()
	{
		LeaveCacheFileFromPreviousRun();
		var watchdog = Create();
		watchdog.Arm(_cacheFile);

		File.WriteAllText(_cacheFile, "written");
		watchdog.OnCacheAccessed(hasStateChanged: true);

		watchdog.TakeResetupRequest().Should().BeFalse();
	}

	[TestMethod]
	public void When_AccessChangedNothing_Then_CheckWaitsForARealWrite()
	{
		var watchdog = Create();
		watchdog.Arm(_cacheFile);

		watchdog.OnCacheAccessed(hasStateChanged: false);
		watchdog.TakeResetupRequest().Should().BeFalse("a read is not a write to verify");

		watchdog.OnCacheAccessed(hasStateChanged: true);
		watchdog.TakeResetupRequest().Should().BeTrue("the check was still armed for the first real write");
	}

	[TestMethod]
	public void When_Checked_Then_LaterWritesAreNotCheckedAgain()
	{
		var watchdog = Create();
		watchdog.Arm(_cacheFile);
		File.WriteAllText(_cacheFile, "written");
		watchdog.OnCacheAccessed(hasStateChanged: true);

		File.Delete(_cacheFile);
		watchdog.OnCacheAccessed(hasStateChanged: true);

		watchdog.TakeResetupRequest().Should().BeFalse("the check is one-shot per storage setup");
	}

	/// <summary>
	/// The retry is bounded to one per process: a re-setup re-runs the persistence probe, which on
	/// macOS is a keychain prompt, so repeating it on every failed write would prompt forever.
	/// </summary>
	[TestMethod]
	public void When_WriteStillFailsAfterTheResetup_Then_NoSecondResetup()
	{
		var watchdog = Create();
		watchdog.Arm(_cacheFile);
		watchdog.OnCacheAccessed(hasStateChanged: true);
		watchdog.TakeResetupRequest().Should().BeTrue();

		watchdog.Arm(_cacheFile);
		watchdog.OnCacheAccessed(hasStateChanged: true);

		watchdog.TakeResetupRequest().Should().BeFalse();
	}
}
