namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// Controls when <c>MsalAuthenticationProvider</c> runs <c>MsalCacheHelper.VerifyPersistence()</c>,
/// the self-check that proves the platform's secure store can round-trip the token cache.
/// </summary>
/// <remarks>
/// The check is not free: it writes, reads and deletes a probe entry in the secure store every time
/// it runs. On macOS it is also unrepeatable by design - MSAL's validation accessor
/// (<c>MacKeychainAccessor.CreateForPersistenceValidation</c>) appends a fresh <see cref="Guid"/> to
/// the keychain service name, so the probe entry has a different name on every launch and macOS
/// re-prompts for keychain access however many times the user answers "Always Allow".
/// </remarks>
internal enum MsalCachePersistenceCheck : byte
{
	/// <summary>
	/// Run the check only when nothing has been persisted at this cache location yet. The default:
	/// a first run is verified up front, and every launch after that makes no extra secure-store
	/// round-trips. Signing out does not reset this - MSAL rewrites an emptied cache rather than
	/// deleting it, so the cache file stays put.
	/// </summary>
	Auto,

	/// <summary>
	/// Run the check on every storage setup.
	/// </summary>
	Always,

	/// <summary>
	/// Never run the check, not even on a first run. A store that cannot be written to is then only
	/// reported once the first real cache write has been attempted.
	/// </summary>
	Never,
}
