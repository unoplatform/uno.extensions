using FluentAssertions;
using Microsoft.Identity.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Authentication.MSAL;

/// <summary>
/// Covers <see cref="MsalRedirectDefaults.Apply"/> - the precedence rules and the resulting value
/// on a genuinely built <see cref="IPublicClientApplication"/>, rather than just the derived string.
/// </summary>
/// <remarks>
/// These assert through <c>IPublicClientApplication.AppConfig.RedirectUri</c>, so they exercise the
/// real MSAL builder rather than a stand-in. That also pins MSAL's own behaviour: if a future MSAL
/// version changes what <c>WithDefaultRedirectUri()</c> yields on .NET, these fail rather than the
/// change reaching consumers silently.
/// </remarks>
[TestClass]
public class Given_MsalRedirectDefaults_Apply
{
	private const string ClientId = "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b";
	private const string BundleId = "com.contoso.myapp";
	private const string WebRedirectUri = "https://contoso.example/authentication-callback";

	/// <summary>Stands in for the Uno WebAuthenticationBroker step, which can't load headless.</summary>
	private static void ApplyWebRedirectUri(PublicClientApplicationBuilder builder)
		=> builder.WithRedirectUri(WebRedirectUri);

	private static MsalConfiguration Config(
		string? redirectUri = null,
		bool useDefaultPlatformRedirectUri = true)
		=> new()
		{
			ClientId = ClientId,
			RedirectUri = redirectUri!,
			UseDefaultPlatformRedirectUri = useDefaultPlatformRedirectUri,
		};

	private static (MsalRedirectDecision Decision, string RedirectUri) Apply(
		MsalConfiguration configuration,
		MsalRedirectPlatform platform,
		string? bundleId = BundleId)
	{
		var builder = PublicClientApplicationBuilder.CreateWithApplicationOptions(configuration);
		var decision = MsalRedirectDefaults.Apply(builder, configuration, platform, bundleId, ApplyWebRedirectUri);
		return (decision, builder.Build().AppConfig.RedirectUri);
	}

	[TestMethod]
	public void When_Android_Then_DerivedFromClientId()
	{
		var (decision, redirectUri) = Apply(Config(), MsalRedirectPlatform.Android);

		decision.Should().Be(MsalRedirectDecision.PlatformDerived);
		redirectUri.Should().Be($"msal{ClientId}://auth");
	}

	[TestMethod]
	public void When_IOS_Then_DerivedFromBundleId()
	{
		var (decision, redirectUri) = Apply(Config(), MsalRedirectPlatform.IOS);

		decision.Should().Be(MsalRedirectDecision.PlatformDerived);
		redirectUri.Should().Be($"msauth.{BundleId}://auth");
	}

	[TestMethod]
	public void When_WebAssembly_Then_BrokerUriApplied()
	{
		var (decision, redirectUri) = Apply(Config(), MsalRedirectPlatform.WebAssembly);

		decision.Should().Be(MsalRedirectDecision.WebAuthenticationBroker);
		redirectUri.Should().Be(WebRedirectUri);
	}

	[TestMethod]
	public void When_Desktop_Then_MsalLoopbackDefault()
	{
		var (decision, redirectUri) = Apply(Config(), MsalRedirectPlatform.Desktop);

		decision.Should().Be(MsalRedirectDecision.MsalDefault);
		// The system-browser flow requires a loopback URI; MSAL rejects anything else on .NET.
		redirectUri.Should().Be("http://localhost");
	}

	[TestMethod]
	public void When_BrokerManaged_Then_NothingApplied()
	{
		// WinAppSDK: the WAM broker owns the redirect URI, so the provider must not touch it.
		var (decision, redirectUri) = Apply(Config(), MsalRedirectPlatform.BrokerManaged);

		decision.Should().Be(MsalRedirectDecision.BrokerManaged);
		redirectUri.Should().NotBe("http://localhost");
	}

	[TestMethod]
	public void When_IOS_WithoutBundleId_Then_FallsBackToMsalDefault()
	{
		var (decision, redirectUri) = Apply(Config(), MsalRedirectPlatform.IOS, bundleId: null);

		decision.Should().Be(MsalRedirectDecision.MsalDefault);
		redirectUri.Should().Be("http://localhost");
	}

	[TestMethod]
	public void When_ConfigurationSuppliesRedirectUri_Then_ConfigurationWins()
	{
		// Configuration outranks the platform default on every platform, including the two that
		// would otherwise derive a value.
		foreach (var platform in new[]
		{
			MsalRedirectPlatform.Android,
			MsalRedirectPlatform.IOS,
			MsalRedirectPlatform.WebAssembly,
			MsalRedirectPlatform.Desktop,
		})
		{
			var (decision, redirectUri) = Apply(Config(redirectUri: "configured://auth"), platform);

			decision.Should().Be(MsalRedirectDecision.ConfigurationSupplied, $"platform {platform}");
			redirectUri.Should().Be("configured://auth", $"platform {platform}");
		}
	}

	[TestMethod]
	public void When_DefaultsDisabled_Then_NothingApplied()
	{
		var (decision, redirectUri) = Apply(
			Config(useDefaultPlatformRedirectUri: false),
			MsalRedirectPlatform.Android);

		decision.Should().Be(MsalRedirectDecision.Disabled);
		redirectUri.Should().NotBe($"msal{ClientId}://auth");
	}

	[TestMethod]
	public void When_BuilderCallbackRunsAfterDefaults_Then_CallbackWins()
	{
		// The provider invokes the app's Builder(...) callback *after* Apply, so an app setting its
		// own redirect URI must win. This is the ordering that changed in the provider - previously
		// WithWebRedirectUri() ran after the callback and stomped the app on WebAssembly.
		var configuration = Config();
		var builder = PublicClientApplicationBuilder.CreateWithApplicationOptions(configuration);

		MsalRedirectDefaults.Apply(builder, configuration, MsalRedirectPlatform.Android, BundleId, ApplyWebRedirectUri);
		builder.WithRedirectUri("app-callback://auth");

		builder.Build().AppConfig.RedirectUri.Should().Be("app-callback://auth");
	}

	[TestMethod]
	public void When_WebAssembly_Then_CallbackStillWins()
	{
		// The WebAssembly case is the one this ordering was changed for; guard it explicitly.
		var configuration = Config();
		var builder = PublicClientApplicationBuilder.CreateWithApplicationOptions(configuration);

		MsalRedirectDefaults.Apply(builder, configuration, MsalRedirectPlatform.WebAssembly, BundleId, ApplyWebRedirectUri);
		builder.WithRedirectUri("https://contoso.example/custom-callback.htm");

		builder.Build().AppConfig.RedirectUri.Should().Be("https://contoso.example/custom-callback.htm");
	}
}
