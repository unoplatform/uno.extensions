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

namespace Uno.Extensions.Authentication.MSAL.UI.Tests;

/// <summary>
/// Covers which <see cref="IKeyValueStorage"/> the host defaults to, which is what decides whether
/// the token caches survive a page reload on WebAssembly - see spec 011.
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
public class Given_BrowserTokenCacheStorage
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
	/// <c>AddMsal</c>.
	/// </remarks>
	private static string DefaultStorageName(string? cacheLocation)
	{
		var values = new Dictionary<string, string?>();
		if (cacheLocation is not null)
		{
			values[CacheLocationKey] = cacheLocation;
		}

		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_BrowserTokenCacheStorage).Assembly)
			.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(values))
			.Build();

		return host.Services.GetRequiredDefaultInstance<IKeyValueStorage>().GetType().Name;
	}

	[TestMethod]
	public void When_Not_Configured_Then_Browser_Uses_SessionStorage()
	{
		var name = DefaultStorageName(null);

		if (OperatingSystem.IsBrowser())
		{
			name.Should().Be("SessionStorageKeyValueStorage", "the default matches MSAL.js: survives a reload, dropped when the tab closes");
		}
		else
		{
			name.Should().NotBe("SessionStorageKeyValueStorage", "sessionStorage only exists in a browser");
		}
	}

	[TestMethod]
	public void When_LocalStorage_Then_Browser_Uses_ApplicationData()
	{
		var name = DefaultStorageName("LocalStorage");

		if (OperatingSystem.IsBrowser())
		{
			// ApplicationData.Current.LocalSettings is localStorage in the browser.
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
		// Configuration binding is case-insensitive, so an app writing msal-browser's own
		// "sessionStorage"/"localStorage" spelling must not silently fall back to the default.
		var name = DefaultStorageName("localStorage");

		if (OperatingSystem.IsBrowser())
		{
			name.Should().Be("ApplicationDataKeyValueStorage");
		}
		else
		{
			name.Should().Be(DefaultStorageName(null));
		}
	}

	[TestMethod]
	public void When_Configured_With_Unknown_Value_Then_Fails_Loudly()
	{
		// The value is a bound enum on KeyValueStorageConfiguration, so configuration binding
		// rejects a typo rather than quietly choosing for you. That is the right trade for a setting
		// that decides where refresh tokens live: silently falling back to sessionStorage when the
		// app asked for memoryStorage is a security-relevant surprise, and it would be invisible.
		FluentActions.Invoking(() => DefaultStorageName("NotACacheLocation"))
			.Should().Throw<Exception>()
			// Literal, not nameof: the enum is internal to Uno.Extensions.Storage.
			.Which.ToString().Should().Contain("BrowserCacheLocation",
				"the failure has to name the setting that is wrong");
	}

	[TestMethod]
	public async Task When_Value_Written_Then_Round_Trips_Through_Default_Storage()
	{
		// The end-to-end check for SessionStorageKeyValueStorage on the WebAssembly lane: on any
		// other head this exercises that head's own default store, which is equally valid.
		var key = $"{nameof(Given_BrowserTokenCacheStorage)}_{nameof(When_Value_Written_Then_Round_Trips_Through_Default_Storage)}";
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

		using var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_BrowserTokenCacheStorage).Assembly)
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
		(await storage.GetAsync<string>(key, cts.Token)).Should().BeNull();
	}
}
