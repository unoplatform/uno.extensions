using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Authentication.MSAL;

[TestClass]
public class Given_MsalRedirectDefaults
{
	private const string ClientId = "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b";
	private const string BundleId = "com.contoso.myapp";

	[TestMethod]
	public void When_Android_Then_DerivedFromClientId()
	{
		var uri = MsalRedirectDefaults.GetPlatformRedirectUri(ClientId, BundleId, isAndroid: true, isIOS: false);

		// Must match the scheme/host an app declares on its BrowserTabActivity intent filter.
		uri.Should().Be($"msal{ClientId}://auth");
	}

	[TestMethod]
	public void When_Android_WithoutClientId_Then_Null()
	{
		// A bare "msal://auth" scheme would collide across every app on the device, so there is
		// nothing safe to derive - the caller falls back to MSAL's own default.
		MsalRedirectDefaults.GetPlatformRedirectUri(null, BundleId, isAndroid: true, isIOS: false).Should().BeNull();
		MsalRedirectDefaults.GetPlatformRedirectUri("", BundleId, isAndroid: true, isIOS: false).Should().BeNull();
	}

	[TestMethod]
	public void When_IOS_Then_DerivedFromBundleId()
	{
		var uri = MsalRedirectDefaults.GetPlatformRedirectUri(ClientId, BundleId, isAndroid: false, isIOS: true);

		// MSAL's documented iOS default, and what the Info.plist CFBundleURLTypes entry declares.
		uri.Should().Be($"msauth.{BundleId}://auth");
	}

	[TestMethod]
	public void When_IOS_WithoutBundleId_Then_Null()
	{
		MsalRedirectDefaults.GetPlatformRedirectUri(ClientId, null, isAndroid: false, isIOS: true).Should().BeNull();
		MsalRedirectDefaults.GetPlatformRedirectUri(ClientId, "", isAndroid: false, isIOS: true).Should().BeNull();
	}

	[TestMethod]
	public void When_Android_Then_BundleIdIgnored()
	{
		var withBundle = MsalRedirectDefaults.GetPlatformRedirectUri(ClientId, BundleId, isAndroid: true, isIOS: false);
		var withoutBundle = MsalRedirectDefaults.GetPlatformRedirectUri(ClientId, null, isAndroid: true, isIOS: false);

		withBundle.Should().Be(withoutBundle);
	}

	[TestMethod]
	public void When_Android_Wins_Over_IOS()
	{
		// Both flags true is not a real configuration, but the Android branch is evaluated first
		// and must stay deterministic.
		var uri = MsalRedirectDefaults.GetPlatformRedirectUri(ClientId, BundleId, isAndroid: true, isIOS: true);

		uri.Should().Be($"msal{ClientId}://auth");
	}

	[TestMethod]
	public void When_DesktopOrOther_Then_Null()
	{
		// Nothing to derive: the provider falls back to MSAL's WithDefaultRedirectUri(), which
		// yields http://localhost on .NET for the system-browser flow.
		MsalRedirectDefaults.GetPlatformRedirectUri(ClientId, BundleId, isAndroid: false, isIOS: false).Should().BeNull();
	}

	[TestMethod]
	public void When_Derived_Then_DistinctPerApp()
	{
		MsalRedirectDefaults.GetPlatformRedirectUri("client-a", null, isAndroid: true, isIOS: false)
			.Should().NotBe(MsalRedirectDefaults.GetPlatformRedirectUri("client-b", null, isAndroid: true, isIOS: false));

		MsalRedirectDefaults.GetPlatformRedirectUri(null, "com.contoso.app1", isAndroid: false, isIOS: true)
			.Should().NotBe(MsalRedirectDefaults.GetPlatformRedirectUri(null, "com.contoso.app2", isAndroid: false, isIOS: true));
	}

	[TestMethod]
	public void When_Derived_Then_ParsesAsAbsoluteUri()
	{
		foreach (var uri in new[]
		{
			MsalRedirectDefaults.GetPlatformRedirectUri(ClientId, null, isAndroid: true, isIOS: false),
			MsalRedirectDefaults.GetPlatformRedirectUri(null, BundleId, isAndroid: false, isIOS: true),
		})
		{
			// MSAL rejects a redirect URI it can't parse, so guard the format itself.
			Uri.TryCreate(uri, UriKind.Absolute, out var parsed).Should().BeTrue();
			parsed!.Host.Should().Be("auth");
		}
	}
}
