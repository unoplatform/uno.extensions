using System.Linq;

namespace Uno.Extensions.Hosting;

public static class UnoHost
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
				if (ctx.HostingEnvironment is IHasAddressBar addressBarHost)
				{
					services.AddSingleton(addressBarHost);
				}
				services.AddSingleton<IStorage, Storage>();
			})
#if __WASM__
				.ConfigureHostConfiguration(config =>
				{
					if (Foundation.WebAssemblyRuntime.IsWebAssembly)
					{
						var href = Foundation.WebAssemblyRuntime.InvokeJS("window.location.href");
						var appsettingsPrefix = new Dictionary<string, string>
							{
								{ HostingConstants.AppSettingsPrefixKey, "local" },
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
			.ConfigureServices((ctx, services) => services.Configure<HostConfiguration>(ctx.Configuration.GetSection(nameof(HostConfiguration))));
	}
}
