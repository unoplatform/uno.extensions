// Uno.Extensions.Authentication.MSAL.Tests compiles this as linked source (the WinUI assembly
// can't load in a plain test host), so keep it free of Uno dependencies: platform selection is
// passed in, never detected here, and the one Uno-only step (the WebAuthenticationBroker URI)
// arrives as a delegate.
//
// Apply() is gated on UNO_EXT_MSAL - the same allow-list the .csproj uses - because it reads
// MsalConfiguration members inherited from PublicClientApplicationOptions, and that base type is
// only applied on the platforms that ship a functional provider. The test project defines
// UNO_EXT_MSAL so it still gets full coverage. Same shape as MsalStorageDefaults.
namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// Which redirect-URI rule <c>MsalRedirectDefaults.Apply</c> applied. Returned so the caller can
/// log the outcome without the helper taking a logger dependency.
/// </summary>
internal enum MsalRedirectDecision
{
	/// <summary>Suppressed by <see cref="MsalConfiguration.UseDefaultPlatformRedirectUri"/>.</summary>
	Disabled,

	/// <summary>The app supplied <c>RedirectUri</c> in configuration; left untouched.</summary>
	ConfigurationSupplied,

	/// <summary>Derived from Uno's WebAuthenticationBroker (WebAssembly).</summary>
	WebAuthenticationBroker,

	/// <summary>Derived from the client id (Android) or bundle id (iOS).</summary>
	PlatformDerived,

	/// <summary>Delegated to MSAL's <c>WithDefaultRedirectUri()</c>.</summary>
	MsalDefault,

	/// <summary>Left to the WAM broker (WinAppSDK).</summary>
	BrokerManaged,
}

/// <summary>
/// The platform whose redirect-URI convention should be applied. Computed by the caller from
/// compile-time symbols and <c>PlatformHelper</c>, so this type stays testable.
/// </summary>
internal enum MsalRedirectPlatform
{
	/// <summary>Desktop (Windows/macOS/Linux under Skia) and anything not listed below.</summary>
	Desktop,

	/// <summary>Android.</summary>
	Android,

	/// <summary>iOS.</summary>
	IOS,

	/// <summary>WebAssembly.</summary>
	WebAssembly,

	/// <summary>WinAppSDK, where the WAM broker owns the redirect URI.</summary>
	BrokerManaged,
}

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

#if UNO_EXT_MSAL
	/// <summary>
	/// Applies the platform's conventional redirect URI to <paramref name="builder"/>, honouring
	/// the precedence rules, and reports which rule fired.
	/// </summary>
	/// <remarks>
	/// Precedence, lowest to highest: the platform default applied here, then <c>RedirectUri</c>
	/// from configuration, then the app's <c>Builder(...)</c> callback - which the caller invokes
	/// *after* this and which therefore simply overwrites whatever was set.
	/// </remarks>
	/// <param name="builder">The builder to configure.</param>
	/// <param name="configuration">The bound MSAL configuration section.</param>
	/// <param name="platform">The platform whose convention should apply.</param>
	/// <param name="bundleId">The iOS bundle identifier; ignored on other platforms.</param>
	/// <param name="applyWebRedirectUri">
	/// Applies the WebAuthenticationBroker-derived URI. Supplied as a delegate because it reaches
	/// into Uno, which can't load in the unit-test host.
	/// </param>
	internal static MsalRedirectDecision Apply(
		PublicClientApplicationBuilder builder,
		MsalConfiguration configuration,
		MsalRedirectPlatform platform,
		string? bundleId,
		Action<PublicClientApplicationBuilder> applyWebRedirectUri)
	{
		if (!configuration.UseDefaultPlatformRedirectUri)
		{
			return MsalRedirectDecision.Disabled;
		}

		if (configuration.RedirectUri is { Length: > 0 })
		{
			// CreateWithApplicationOptions already applied it; don't overwrite a configured value.
			return MsalRedirectDecision.ConfigurationSupplied;
		}

		if (platform == MsalRedirectPlatform.WebAssembly)
		{
			applyWebRedirectUri(builder);
			return MsalRedirectDecision.WebAuthenticationBroker;
		}

		var platformRedirectUri = GetPlatformRedirectUri(
			configuration.ClientId,
			bundleId,
			isAndroid: platform == MsalRedirectPlatform.Android,
			isIOS: platform == MsalRedirectPlatform.IOS);

		if (platformRedirectUri is { Length: > 0 })
		{
			builder.WithRedirectUri(platformRedirectUri);
			return MsalRedirectDecision.PlatformDerived;
		}

		if (platform == MsalRedirectPlatform.BrokerManaged)
		{
			// The WAM broker owns the redirect URI on WinAppSDK; leave it alone.
			return MsalRedirectDecision.BrokerManaged;
		}

		// Desktop and anything else: http://localhost on .NET, which is what the system-browser
		// flow needs (https://aka.ms/msal-net-default-reply-uri).
		builder.WithDefaultRedirectUri();
		return MsalRedirectDecision.MsalDefault;
	}
#endif

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
