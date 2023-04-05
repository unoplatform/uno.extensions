namespace Uno.Extensions.Hosting;

public static class UnoHost
{
	public static IHostBuilder CreateDefaultBuilder(string[]? args = null)
	{
		var callingAssembly = Assembly.GetCallingAssembly();
		return new HostBuilder()
			.ConfigureCustomDefaults(args)
			.ConfigureAppConfiguration((ctx, appConfig) =>
			{
				string dataFolder = string.Empty;
				try
				{
					dataFolder = Windows.Storage.ApplicationData.Current?.LocalFolder?.Path ?? string.Empty;
				}
				catch
				{
					// This will throw an exception on WinUI if unpackaged, so dataFolder will be null
				}
#if WINUI && WINDOWS
				var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
				if (string.IsNullOrWhiteSpace(dataFolder))
				{
					dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), appName);
				}
				if (!Directory.Exists(dataFolder))
				{
					Directory.CreateDirectory(dataFolder);
				}
#endif
				var appHost = AppHostingEnvironment.FromHostEnvironment(ctx.HostingEnvironment, dataFolder, callingAssembly);
				ctx.HostingEnvironment = appHost;
			})
			.ConfigureServices((ctx, services) =>
			{
				if (ctx.HostingEnvironment is IAppHostEnvironment appHost)
				{
					services.AddSingleton(appHost);
				}
				if (ctx.HostingEnvironment is IDataFolderProvider dataProvider)
				{
					services.AddSingleton(dataProvider);
				}
				if (ctx.HostingEnvironment is IHasAddressBar addressBarHost)
				{
					services.AddSingleton(addressBarHost);
				}
			})
#if __WASM__
				.ConfigureHostConfiguration(config =>
				{
					if (Foundation.WebAssemblyRuntime.IsWebAssembly)
					{
						var href = Foundation.WebAssemblyRuntime.InvokeJS("window.location.href");
						var appsettingsPrefix = new Dictionary<string, string>
							{
								{ HostingConstants.AppConfigPrefixKey, "local" },
								{ HostingConstants.LaunchUrlKey, href }
							};
						config.AddInMemoryCollection(appsettingsPrefix);

						var query = new Uri(href).Query;
						var queriesValues = System.Web.HttpUtility.ParseQueryString(query);
						var queryDict = (from k in queriesValues.AllKeys
										 select new { Key = k, Value = queriesValues[k] }).ToDictionary(x => x.Key, x => x.Value);
						config.AddInMemoryCollection(queryDict);
					}
				})
#endif
			.ConfigureServices((ctx, services) => services.Configure<HostConfiguration>(ctx.Configuration.GetSection(nameof(HostConfiguration))))
			.UseStorage();
	}
}
