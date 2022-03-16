
namespace Uno.Extensions.Hosting;

public static class Host
{
	public static IHostBuilder CreateDefaultBuilder(string[]? args = null)
	{
		return new HostBuilder()
			.ConfigureCustomDefaults(args)
			.ConfigureAppConfiguration((ctx, appConfig) =>
			{
				var appHost = AppHostingEnvironment.FromHostEnvironment(ctx.HostingEnvironment, Windows.Storage.ApplicationData.Current.LocalFolder.Path);
				ctx.HostingEnvironment = appHost;
			})
			.ConfigureServices((ctx, services) =>
			{
				if (ctx.HostingEnvironment is IAppHostEnvironment appHost)
				{
					services.AddSingleton(appHost);
				}
				services.AddSingleton<IStorageProxy, StorageProxy>();
			})
#if __WASM__
				.ConfigureHostConfiguration(config =>
				{
					if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_MONO_RUNTIME_MODE")))
					{
						var href = Foundation.WebAssemblyRuntime.InvokeJS("window.location.href");
						var appsettingsPrefix = new Dictionary<string, string>
							{
								{ HostingConstants.AppSettingsPrefixKey, "local" },
								{ HostingConstants.LaunchUrlKey, href }
							};
						config.AddInMemoryCollection(appsettingsPrefix);
					}
				})
#endif
			.ConfigureServices((ctx, services) => services.Configure<HostConfiguration>(ctx.Configuration.GetSection(nameof(HostConfiguration))));
	}
}
