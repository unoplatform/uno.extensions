using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Storage.KeyValueStorage;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Storage.UI.Tests;

/// <summary>
/// Covers which <see cref="IKeyValueStorage"/> the host defaults to - the store everything built on
/// storage (most visibly the authentication token cache) writes through, and on WebAssembly what
/// decides whether that data survives a page reload.
/// </summary>
/// <remarks>
/// Runs on every head on purpose. On WebAssembly it asserts the mapping from
/// <c>KeyValueStorageConfiguration:BrowserCacheLocation</c> to a store; everywhere else it asserts it is
/// <em>ignored</em>, which is the regression guard for the spec's scope guard: Skia desktop shares
/// the same <c>#else</c> branch as the browser, and following the setting there would turn DPAPI /
/// Keychain / libsecret into cleartext.
/// </remarks>
[TestClass]
[RunsOnUIThread]
public class Given_DefaultKeyValueStorage
{
	private const string CacheLocationKey = "KeyValueStorageConfiguration:BrowserCacheLocation";

	/// <summary>
	/// Builds a host and reports the type name of the default key-value storage.
	/// </summary>
	/// <remarks>
	/// The type name rather than the type: every storage provider is internal to
	/// Uno.Extensions.Storage.UI. <see cref="UnoHost.CreateDefaultBuilder(System.Reflection.Assembly, string[])"/>
	/// calls <c>UseStorage()</c> itself, before this configuration is added - which is exactly the
	/// ordering the selection has to survive, since an app is free to call <c>UseStorage</c> before
	/// whatever registers the configuration that names the store.
	/// </remarks>
	private static string DefaultStorageName(string? cacheLocation)
	{
		var values = new Dictionary<string, string?>();
		if (cacheLocation is not null)
		{
			values[CacheLocationKey] = cacheLocation;
		}

		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_DefaultKeyValueStorage).Assembly)
			.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(values))
			.Build();

		return host.Services.GetRequiredDefaultInstance<IKeyValueStorage>().GetType().Name;
	}

	[TestMethod]
	public void When_Not_Configured_Then_Browser_Uses_LocalStorage()
	{
		var name = DefaultStorageName(null);

		if (OperatingSystem.IsBrowser())
		{
			// ApplicationData.Current.LocalSettings is localStorage in the browser - the store
			// WebAssembly used before this setting existed. Defaulting anywhere else would relocate
			// every existing app's key-value data on a package upgrade.
			name.Should().Be("ApplicationDataKeyValueStorage");
		}
		else
		{
			name.Should().NotBe("SessionStorageKeyValueStorage", "sessionStorage only exists in a browser");
		}
	}

	[TestMethod]
	public void When_SessionStorage_Then_Browser_Uses_SessionStorage()
	{
		var name = DefaultStorageName("SessionStorage");

		if (OperatingSystem.IsBrowser())
		{
			name.Should().Be("SessionStorageKeyValueStorage", "the tighter session-scoped lifetime is opt-in");
		}
		else
		{
			name.Should().Be(DefaultStorageName(null), "the browser cache location must not move the store on any other platform");
		}
	}

	[TestMethod]
	public void When_LocalStorage_Then_Browser_Uses_ApplicationData()
	{
		var name = DefaultStorageName("LocalStorage");

		if (OperatingSystem.IsBrowser())
		{
			name.Should().Be("ApplicationDataKeyValueStorage");
		}
		else
		{
			name.Should().Be(DefaultStorageName(null), "the browser cache location must not move the store on any other platform");
		}
	}

	[TestMethod]
	public void When_MemoryStorage_Then_Browser_Writes_Nothing_Persistent()
	{
		var name = DefaultStorageName("MemoryStorage");

		if (OperatingSystem.IsBrowser())
		{
			name.Should().Be("InMemoryKeyValueStorage", "MemoryStorage is the pre-011 posture: nothing reaches browser storage");
		}
		else
		{
			name.Should().Be(DefaultStorageName(null), "the browser cache location must not move the store on any other platform");
		}
	}

	[TestMethod]
	public void When_Configured_With_Other_Casing_Then_Still_Honored()
	{
		// Case-insensitive to match what configuration binding would have accepted, so an app
		// writing msal-browser's own "sessionStorage" spelling is not silently ignored.
		var name = DefaultStorageName("sessionStorage");

		if (OperatingSystem.IsBrowser())
		{
			name.Should().Be("SessionStorageKeyValueStorage");
		}
		else
		{
			name.Should().Be(DefaultStorageName(null));
		}
	}

	[TestMethod]
	public void When_Configured_With_Unknown_Value_Then_Throws_Naming_The_Setting()
	{
		// A single strict reader in AddKeyedStorage rejects the value while the host is being built,
		// rather than leaving it to whether something later happens to bind an options type. A typo
		// silently choosing where credentials are kept is the one outcome worth failing startup for.
		FluentActions.Invoking(() => DefaultStorageName("NotACacheLocation"))
			.Should().Throw<InvalidOperationException>()
			// Literals, not nameof: the enum is internal to Uno.Extensions.Storage.
			.WithMessage("*BrowserCacheLocation*")
			.Which.Message.Should().Contain("SessionStorage").And.Contain("LocalStorage").And.Contain("MemoryStorage",
				"the failure has to name the setting and its legal values");
	}

	[TestMethod]
	public void When_Configured_With_Numeric_Value_Then_Throws()
	{
		// Enum.TryParse accepts any numeric string, in range or out of it, so "3" used to arrive as
		// an undefined enum value and fall through to the default - the "invalid values are
		// rejected" guarantee failed for exactly the input a hand-edited config is likeliest to
		// contain. Enum.IsDefined is what closes it.
		FluentActions.Invoking(() => DefaultStorageName("3"))
			.Should().Throw<InvalidOperationException>().WithMessage("*BrowserCacheLocation*");
	}

	[TestMethod]
	public async Task When_Value_Written_Then_Round_Trips_Through_Default_Storage()
	{
		// The end-to-end check for SessionStorageKeyValueStorage on the WebAssembly lane: on any
		// other head this exercises that head's own default store, which is equally valid.
		var key = $"{nameof(Given_DefaultKeyValueStorage)}_{nameof(When_Value_Written_Then_Round_Trips_Through_Default_Storage)}";
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_DefaultKeyValueStorage).Assembly)
			.Build();
		var storage = host.Services.GetRequiredDefaultInstance<IKeyValueStorage>();

		try
		{
			await storage.SetAsync(key, "the-value", cts.Token);

			(await storage.GetAsync<string>(key, cts.Token)).Should().Be("the-value");
			(await storage.GetKeysAsync(cts.Token)).Should().Contain(key);
		}
		finally
		{
			// The store is the app's real one on every head, so leave nothing behind.
			await storage.ClearAsync(key, cts.Token);
		}

		(await storage.GetKeysAsync(cts.Token)).Should().NotContain(key);

		// Deliberately not asserting what a *read* of the cleared key does. IKeyValueStorage.GetAsync
		// is documented to throw KeyNotFoundException when the value does not exist, and the platform
		// stores disagree: KeyChainKeyValueStorage honours it (KeyChainKeyValueStorage.cs, end of
		// InternalGetAsync), while ApplicationDataKeyValueStorage returns default instead. Asserting
		// either one pins a platform accident - this assertion read `.Should().BeNull()` and passed on
		// desktop while failing the iOS lane. GetKeysAsync is the part of the contract both stores
		// agree on, and it is what TokenCache.HasTokenAsync actually relies on. The divergence itself
		// is a product finding, tracked in specs/011-wasm-msal-token-cache/progress.md.
	}
}
