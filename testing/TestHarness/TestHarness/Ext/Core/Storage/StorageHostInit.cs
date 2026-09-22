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
						var unpackaged = sp.GetRequiredService<ISettings>();
						return new TestingKeyValueStorage(l, inmem, settings, s, unpackaged);
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

// Uno 7 ships no android/ios flavour of Uno.WinUI, so Uno.Extensions.Storage.UI builds for net10.0
// only and the KeyStore / KeyChain stores no longer exist on any head. Every platform now falls back
// to ApplicationDataKeyValueStorage, which is what the Skia mobile heads were already getting.
internal record TestingKeyValueStorage(
	ILogger<TestingKeyValueStorage> TestingLogger,
	InMemoryKeyValueStorage InMemoryStorage,
	KeyValueStorageSettings Settings,
	ISerializer Serializer,
	ISettings UnpackagedSettings) : ApplicationDataKeyValueStorage(TestingLogger, InMemoryStorage, Settings, Serializer, UnpackagedSettings);
