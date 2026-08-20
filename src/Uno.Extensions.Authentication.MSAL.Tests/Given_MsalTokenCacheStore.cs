using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.Extensions.Authentication.MSAL;

[TestClass]
public class Given_MsalTokenCacheStore
{
	private const string ClientId = "161a9fb5-3b16-487a-81a2-ac45dcc0ad3b";

	/// <summary>
	/// The prefix <c>Uno.Extensions.Authentication.TokenCache</c> uses for every entry it owns.
	/// Duplicated as a literal on purpose: that type is private to another assembly, and this test
	/// exists to fail if either side of the contract drifts.
	/// </summary>
	private const string TokenCachePrefix = "AuthToken_";

	private static MsalTokenCacheStore CreateStore(FakeKeyValueStorage storage, string? clientId = ClientId) =>
		new(storage, NullLogger.Instance, clientId);

	[TestMethod]
	public async Task When_Saved_Then_Load_Returns_Same_Bytes()
	{
		var storage = new FakeKeyValueStorage();
		var store = CreateStore(storage);
		var blob = new byte[] { 0x00, 0x01, 0xFE, 0xFF, 0x42 };

		await store.SaveAsync(blob, CancellationToken.None);
		var loaded = await store.LoadAsync(CancellationToken.None);

		loaded.Should().Equal(blob);
	}

	[TestMethod]
	public async Task When_Nothing_Stored_Then_Load_Returns_Null()
	{
		var store = CreateStore(new FakeKeyValueStorage());

		var loaded = await store.LoadAsync(CancellationToken.None);

		loaded.Should().BeNull();
	}

	[TestMethod]
	public async Task When_Blob_Is_Empty_Then_Entry_Is_Removed()
	{
		var storage = new FakeKeyValueStorage();
		var store = CreateStore(storage);
		await store.SaveAsync(new byte[] { 0x01 }, CancellationToken.None);

		await store.SaveAsync(Array.Empty<byte>(), CancellationToken.None);

		// MSAL serializes an empty cache once the last account is removed; storing that placeholder
		// would leave a key behind that outlives the sign-in it belonged to.
		storage.Values.Should().NotContainKey(store.Key);
		(await store.LoadAsync(CancellationToken.None)).Should().BeNull();
	}

	[TestMethod]
	public async Task When_Blob_Is_Null_Then_Entry_Is_Removed()
	{
		var storage = new FakeKeyValueStorage();
		var store = CreateStore(storage);
		await store.SaveAsync(new byte[] { 0x01 }, CancellationToken.None);

		await store.SaveAsync(null, CancellationToken.None);

		storage.Values.Should().NotContainKey(store.Key);
	}

	[TestMethod]
	public async Task When_Stored_Value_Is_Corrupt_Then_Load_Discards_It()
	{
		var storage = new FakeKeyValueStorage();
		var store = CreateStore(storage);
		// Reachable without a bug on our side: browser storage is editable by the user and by any
		// script on the origin.
		storage.Values[store.Key] = "not valid base64 !!";

		var loaded = await store.LoadAsync(CancellationToken.None);

		loaded.Should().BeNull();
		storage.Values.Should().NotContainKey(store.Key, "a value that can't be decoded must not be handed to DeserializeMsalV3 on every subsequent token call");
	}

	[TestMethod]
	public async Task When_Cleared_Then_Entry_Is_Removed()
	{
		var storage = new FakeKeyValueStorage();
		var store = CreateStore(storage);
		await store.SaveAsync(new byte[] { 0x01 }, CancellationToken.None);

		await store.ClearAsync(CancellationToken.None);

		storage.Values.Should().NotContainKey(store.Key);
	}

	[TestMethod]
	public async Task When_Clearing_Absent_Entry_Then_Nothing_Happens()
	{
		var storage = new FakeKeyValueStorage();
		var store = CreateStore(storage);

		await store.ClearAsync(CancellationToken.None);

		storage.Values.Should().BeEmpty();
		storage.WriteCount.Should().Be(0);
	}

	[TestMethod]
	public void When_Key_Built_Then_It_Does_Not_Collide_With_TokenCache_Prefix()
	{
		// TokenCache.GetAsync returns every key starting with its prefix and the result is handed
		// to the HTTP handlers as a map of tokens - a colliding key would be sent upstream as if it
		// were a bearer token.
		MsalTokenCacheStore.GetKey(ClientId).Should().NotStartWith(TokenCachePrefix);
		MsalTokenCacheStore.GetKey(null).Should().NotStartWith(TokenCachePrefix);
		MsalTokenCacheStore.KeyPrefix.Should().NotStartWith(TokenCachePrefix);
	}

	[TestMethod]
	public void When_ClientId_Differs_Then_Keys_Differ()
	{
		// Caches for different app registrations must not overwrite each other.
		MsalTokenCacheStore.GetKey(ClientId)
			.Should().NotBe(MsalTokenCacheStore.GetKey("00000000-0000-0000-0000-000000000000"));
	}

	[TestMethod]
	public void When_ClientId_Missing_Then_Key_Is_Still_Well_Formed()
	{
		MsalTokenCacheStore.GetKey(null).Should().Be($"{MsalTokenCacheStore.KeyPrefix}{MsalTokenCacheStore.DefaultKeySuffix}");
		MsalTokenCacheStore.GetKey(string.Empty).Should().Be(MsalTokenCacheStore.GetKey(null));
	}

	[TestMethod]
	public async Task When_Saved_Twice_Then_Latest_Blob_Wins()
	{
		var storage = new FakeKeyValueStorage();
		var store = CreateStore(storage);
		await store.SaveAsync(new byte[] { 0x01, 0x02 }, CancellationToken.None);

		var second = new byte[] { 0x03, 0x04, 0x05 };
		await store.SaveAsync(second, CancellationToken.None);

		(await store.LoadAsync(CancellationToken.None)).Should().Equal(second);
	}
}
