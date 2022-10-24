using System.Collections.Immutable;
using Microsoft.Extensions.Options;
using Uno.Extensions.Storage.KeyValueStorage;

namespace TestHarness;

public class StorageHostInit : BaseHostInitialization
{
	protected override string[] ConfigurationFiles => new string[] { "TestHarness.Ext.Core.Storage.appsettings.storage.json" };

	public const string NoCacheStorage = "NoCache";
	protected override IHostBuilder Custom(IHostBuilder builder)
	{
		return builder
			.ConfigureServices(services => services
			.AddNamedSingleton<IKeyValueStorage, TestingKeyValueStorage>(
					NoCacheStorage,
					sp =>
					{
						var l = sp.GetRequiredService<ILogger<TestingKeyValueStorage>>();
						var inmem = sp.GetRequiredService<InMemoryKeyValueStorage>();
						var s = sp.GetRequiredService<ISerializer>();
						var config = sp.GetRequiredService<IOptions<KeyValueStorageConfiguration>>();
						var settings = config.Value.GetSettingsOrDefault(NoCacheStorage);
						return new TestingKeyValueStorage(l,inmem, settings, s);
					}));
	}



	protected override void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register();


		// RouteMap required for Shell if initialRoute or initialViewModel isn't specified when calling NavigationHost
		routes.Register(
			new RouteMap(""));
	}
}

internal record  TestingKeyValueStorage
#if __ANDROID__
			: KeyStoreKeyValueStorage
			{
	public TestingKeyValueStorage(
		ILogger<TestingKeyValueStorage> logger,
		InMemoryKeyValueStorage inmem,
		KeyValueStorageSettings settings,
		ISerializer serializer) : base(logger, inmem, settings, serializer)
	{

	}
#elif __IOS__
			(ILogger<TestingKeyValueStorage> TestingLogger,
	InMemoryKeyValueStorage InMemoryStorage,
	KeyValueStorageSettings Settings,
	ISerializer Serializer) : KeyChainKeyValueStorage(TestingLogger, InMemoryStorage, Settings, Serializer)
{

#else
			(ILogger<TestingKeyValueStorage> TestingLogger,
	InMemoryKeyValueStorage InMemoryStorage,
	KeyValueStorageSettings Settings,
	ISerializer Serializer) : ApplicationDataKeyValueStorage(TestingLogger, InMemoryStorage, Settings, Serializer)
{
#endif

}


