using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Authentication.MSAL;

[TestClass]
public class Given_MsalStorageDefaults
{
	private const string CacheFileName = "msal.cache";
	private const string CacheFolder = "/fake/app-data/msal";
	private const string ClientId = "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b";

	private static StorageCreationPropertiesBuilder CreateBuilder() =>
		new(CacheFileName, CacheFolder);

	[TestMethod]
	public void When_MacOS_Then_KeychainDefaultsApplied()
	{
		var builder = CreateBuilder();

		MsalStorageDefaults.ApplyDefaults(builder, ClientId, configuredServiceName: null, configuredAccountName: null, isMacOS: true, isLinux: false);
		var properties = builder.Build();

		properties.MacKeyChainServiceName.Should().Be($"uno.extensions.msal.{ClientId}");
		properties.MacKeyChainAccountName.Should().Be("MSALCache");
	}

	[TestMethod]
	public void When_MacOS_WithConfiguredNames_Then_ConfigurationWins()
	{
		var builder = CreateBuilder();

		MsalStorageDefaults.ApplyDefaults(builder, ClientId, configuredServiceName: "com.contoso.myapp", configuredAccountName: "ContosoCache", isMacOS: true, isLinux: false);
		var properties = builder.Build();

		properties.MacKeyChainServiceName.Should().Be("com.contoso.myapp");
		properties.MacKeyChainAccountName.Should().Be("ContosoCache");
	}

	[TestMethod]
	public void When_MacOS_WithEmptyConfiguredNames_Then_TreatedAsUnset()
	{
		var builder = CreateBuilder();

		MsalStorageDefaults.ApplyDefaults(builder, ClientId, configuredServiceName: "", configuredAccountName: "", isMacOS: true, isLinux: false);
		var properties = builder.Build();

		properties.MacKeyChainServiceName.Should().Be($"uno.extensions.msal.{ClientId}");
		properties.MacKeyChainAccountName.Should().Be("MSALCache");
	}

	[TestMethod]
	public void When_MacOS_WithoutClientId_Then_ServiceNameFallsBackToPrefix()
	{
		var builder = CreateBuilder();

		MsalStorageDefaults.ApplyDefaults(builder, clientId: null, configuredServiceName: null, configuredAccountName: null, isMacOS: true, isLinux: false);
		var properties = builder.Build();

		properties.MacKeyChainServiceName.Should().Be("uno.extensions.msal");
	}

	[TestMethod]
	public void When_MacOS_StoreCallbackAfterDefaults_Then_CallbackWins()
	{
		// The provider applies the defaults before invoking the app-supplied Store callback,
		// so an app calling WithMacKeyChain itself must win over the defaults.
		var builder = CreateBuilder();

		MsalStorageDefaults.ApplyDefaults(builder, ClientId, configuredServiceName: null, configuredAccountName: null, isMacOS: true, isLinux: false);
		builder.WithMacKeyChain("app.custom.service", "AppCustomAccount");
		var properties = builder.Build();

		properties.MacKeyChainServiceName.Should().Be("app.custom.service");
		properties.MacKeyChainAccountName.Should().Be("AppCustomAccount");
	}

	[TestMethod]
	public void When_Linux_Then_KeyringDefaultsApplied()
	{
		var builder = CreateBuilder();

		MsalStorageDefaults.ApplyDefaults(builder, ClientId, configuredServiceName: null, configuredAccountName: null, isMacOS: false, isLinux: true);
		var properties = builder.Build();

		properties.KeyringSchemaName.Should().Be("com.unoplatform.extensions.tokencache");
		properties.KeyringCollection.Should().Be(MsalCacheHelper.LinuxKeyRingDefaultCollection);
		properties.KeyringSecretLabel.Should().Be("MSAL token cache for Uno Platform applications");
		properties.KeyringAttribute1.Should().Be(new KeyValuePair<string, string>("MsalClientID", ClientId));
		properties.KeyringAttribute2.Should().Be(new KeyValuePair<string, string>("Product", "Uno.Extensions"));
	}

	[TestMethod]
	public void When_Windows_Then_NoSecureStorageDefaultsApplied()
	{
		var builder = CreateBuilder();

		MsalStorageDefaults.ApplyDefaults(builder, ClientId, configuredServiceName: null, configuredAccountName: null, isMacOS: false, isLinux: false);
		var properties = builder.Build();

		// Windows relies on DPAPI file protection; no keychain/keyring properties are required.
		properties.MacKeyChainServiceName.Should().BeNull();
		properties.KeyringSchemaName.Should().BeNull();
	}

	[TestMethod]
	public void When_ServiceNameDerived_Then_DistinctPerClientId()
	{
		var first = MsalStorageDefaults.GetMacKeychainServiceName(null, "client-a");
		var second = MsalStorageDefaults.GetMacKeychainServiceName(null, "client-b");

		first.Should().NotBe(second);
	}
}
