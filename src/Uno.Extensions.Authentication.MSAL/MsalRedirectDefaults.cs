// Deliberately free of #if branches and of any dependency beyond the BCL:
// Uno.Extensions.Authentication.MSAL.Tests compiles it as linked source (the WinUI assembly
// can't load in a plain test host). Platform selection is passed in, never detected here.
namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// Computes the default redirect URI used by MsalAuthenticationProvider on platforms where MSAL's
/// own <c>WithDefaultRedirectUri()</c> does not produce a usable value.
/// </summary>
/// <remarks>
/// MSAL's <c>WithDefaultRedirectUri()</c> only covers the desktop platforms
/// (<c>http://localhost</c> on .NET Core, the nativeclient URI on .NET Framework); on Android and
/// iOS it leaves the app to supply one. Both values are mechanically derivable, and the derived
/// value is exactly the one the app must already have declared in its
/// <c>AndroidManifest.xml</c> intent filter / <c>Info.plist</c> <c>CFBundleURLTypes</c> entry, so
/// deriving it here removes per-platform boilerplate rather than guessing at it.
/// </remarks>
internal static class MsalRedirectDefaults
{
	// MSAL.NET's BrowserTabActivity convention: an app declaring the matching intent filter uses
	// scheme "msal{ClientId}" and host "auth". These are an app-registration contract - changing
	// them invalidates redirect URIs already registered in Entra.
	internal const string AndroidSchemePrefix = "msal";

	// MSAL's documented iOS default (https://aka.ms/msal-net-default-reply-uri): msauth.{BundleId}://auth.
	internal const string IosSchemePrefix = "msauth.";

	internal const string RedirectHost = "auth";

	/// <summary>
	/// Returns the platform's conventional redirect URI, or <c>null</c> when there isn't one to
	/// derive - in which case the caller should fall back to MSAL's <c>WithDefaultRedirectUri()</c>.
	/// </summary>
	/// <param name="clientId">The MSAL client id; the Android URI is derived from it.</param>
	/// <param name="bundleId">The iOS bundle identifier; the iOS URI is derived from it.</param>
	/// <param name="isAndroid">Whether the app is running on Android.</param>
	/// <param name="isIOS">Whether the app is running on iOS.</param>
	internal static string? GetPlatformRedirectUri(
		string? clientId,
		string? bundleId,
		bool isAndroid,
		bool isIOS)
	{
		if (isAndroid)
		{
			// Without a client id there is nothing to derive from, and a scheme of bare "msal"
			// would collide across every app on the device.
			return clientId is { Length: > 0 }
				? $"{AndroidSchemePrefix}{clientId}://{RedirectHost}"
				: null;
		}

		if (isIOS)
		{
			return bundleId is { Length: > 0 }
				? $"{IosSchemePrefix}{bundleId}://{RedirectHost}"
				: null;
		}

		return null;
	}
}
