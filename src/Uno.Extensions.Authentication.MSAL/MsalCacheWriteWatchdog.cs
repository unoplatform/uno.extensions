// Free of #if branches and of Microsoft.Identity.Client types, like MsalTokenCacheStore and for the
// same reason: Uno.Extensions.Authentication.MSAL.Tests compiles this file as linked source.
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// Confirms that the first token-cache write after a storage setup actually reached the cache
/// file, and asks for one more storage setup when it did not.
/// </summary>
/// <remarks>
/// This is what lets <see cref="MsalCachePersistenceCheck.Auto"/> skip the up-front persistence
/// probe without trading a startup error for silent data loss: <c>MsalCacheHelper</c> logs and
/// swallows write failures ("Could not write the token cache. Ignoring."), so a store that rejects
/// writes would otherwise look healthy until the user found themselves signed out after a restart.
/// <para>
/// A class of its own rather than fields on <c>MsalAuthenticationProvider</c>: that provider is a
/// record and is copied with <c>with</c> while it is configured, and a callback registered on the
/// MSAL token cache stays bound to whichever instance registered it. Every copy shares this one
/// object, so the state the callback writes is the state the provider reads.
/// </para>
/// </remarks>
internal sealed class MsalCacheWriteWatchdog(ILogger logger)
{
	private sealed record Pending(string CacheFilePath, DateTime? LastWriteBeforeUtc);

	/// <summary>The write still owed a check, or <c>null</c>. Taken atomically, so it is checked once.</summary>
	private Pending? _pending;

	/// <summary>
	/// 1 once a failed check has asked for a storage re-setup. Bounds the retry to one per process:
	/// a rebuilt setup that also fails would otherwise re-run the persistence probe - and its
	/// keychain prompts - on every write.
	/// </summary>
	private int _retryConsumed;

	private int _resetupRequested;

	/// <summary>
	/// Arms a one-shot check that the next state-changing cache access writes
	/// <paramref name="cacheFilePath"/>.
	/// </summary>
	public void Arm(string cacheFilePath) =>
		Interlocked.Exchange(ref _pending, new Pending(cacheFilePath, LastWriteUtc(cacheFilePath)));

	/// <summary>
	/// Whether a failed check has asked for storage to be set up again since this was last called.
	/// </summary>
	public bool TakeResetupRequest() => Interlocked.Exchange(ref _resetupRequested, 0) != 0;

	/// <summary>To be called after every token-cache access, with <c>HasStateChanged</c>.</summary>
	public void OnCacheAccessed(bool hasStateChanged)
	{
		// Plain read first: once the check is done this is the only cost on every later access.
		if (!hasStateChanged || _pending is null || Interlocked.Exchange(ref _pending, null) is not { } pending)
		{
			return;
		}

		if (WriteReachedFile(pending.LastWriteBeforeUtc, LastWriteUtc(pending.CacheFilePath)))
		{
			if (logger.IsEnabled(LogLevel.Trace))
			{
				logger.LogTrace("Token-cache write confirmed at {CacheFileName}", Path.GetFileName(pending.CacheFilePath));
			}

			return;
		}

		if (Interlocked.Exchange(ref _retryConsumed, 1) != 0)
		{
			if (logger.IsEnabled(LogLevel.Warning))
			{
				logger.LogWarning("The token cache was serialized but nothing reached {CacheFileName} again after a storage re-setup; not retrying further. Sign-in state won't survive an app restart - the MsalCacheHelper messages above name the cause", Path.GetFileName(pending.CacheFilePath));
			}

			return;
		}

		if (logger.IsEnabled(LogLevel.Error))
		{
			logger.LogError("The token cache was serialized but nothing reached {CacheFileName} - secure storage rejected the write and MsalCacheHelper swallowed the failure (its own message above names the cause). Retrying storage setup once; sign-in state won't survive an app restart until it succeeds", Path.GetFileName(pending.CacheFilePath));
		}

		Volatile.Write(ref _resetupRequested, 1);
	}

	/// <summary>
	/// Whether a write landed: every accessor touches the cache file when it writes - including the
	/// macOS one, whose payload goes to the keychain - so the file must exist and must not be the
	/// untouched file a previous run left behind.
	/// </summary>
	/// <remarks>
	/// Existence alone proves nothing under <see cref="MsalCachePersistenceCheck.Auto"/>, which skips
	/// the probe precisely because the file is already there: a store that has since started
	/// rejecting writes leaves that old file in place.
	/// </remarks>
	internal static bool WriteReachedFile(DateTime? lastWriteBeforeUtc, DateTime? lastWriteAfterUtc) =>
		lastWriteAfterUtc is not null && lastWriteAfterUtc != lastWriteBeforeUtc;

	private static DateTime? LastWriteUtc(string path) =>
		File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;
}
