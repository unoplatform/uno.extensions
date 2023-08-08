namespace Uno.Extensions.Hosting;

/// <summary>
/// Contains helpers to create a HostBuilder that is tailored to multiple target platforms.
/// </summary>
public static class UnoHost
{
	private const string DefaultUnoAppName = "unoapp";

	/// <summary>
	/// Initializes a new instance of the HostBuilder class that is pre-configured 
	/// for multi-platform Uno applications.
	/// </summary>
	/// <param name="args">
	/// The command line arguments.
	/// </param>
	/// <returns>
	/// The initialized IHostBuilder.
	/// </returns>
	public static IHostBuilder CreateDefaultBuilder(string[]? args = null)
	{
		var callingAssembly = Assembly.GetCallingAssembly();
		return CreateDefaultBuilder(callingAssembly, args);
	}

	internal static IHostBuilder CreateDefaultBuilder(Assembly applicationAssembly, string[]? args = null)
	{
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
					// Can also be null on Linux FrameBuffer
				}

				if (string.IsNullOrWhiteSpace(dataFolder))
				{
					var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? DefaultUnoAppName;
					dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), appName);
				}

				if (!string.IsNullOrWhiteSpace(dataFolder) &&
					!Directory.Exists(dataFolder))
				{
					Directory.CreateDirectory(dataFolder);
				}

				var appHost = ctx.HostingEnvironment.FromHostEnvironment(dataFolder, applicationAssembly);
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
