﻿#if __WASM__
using Uno.Foundation;
#endif

namespace Uno.Extensions.Hosting;

internal class AppHostingEnvironment : HostingEnvironment, IAppHostEnvironment, IDataFolderProvider
#if __WASM__
	, IHasAddressBar
#endif
{
	public string? AppDataPath { get; init; }

	public Assembly? HostAssembly { get; init; }

#if __WASM__
	public async Task UpdateAddressBar(Uri applicationUri)
	{
		// Note: This is a hack to avoid error being thrown when loading products async
		await Task.Delay(1000).ConfigureAwait(false);
		CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
					{
						var href = WebAssemblyRuntime.InvokeJS("window.location.href");
						var appUriBuilder = new UriBuilder(applicationUri);
						var url = new UriBuilder(href);
						url.Query = appUriBuilder.Query;
						url.Path = appUriBuilder.Path;
						var webUri = url.Uri.OriginalString;
						var js = $"window.history.pushState(\"{webUri}\",\"\", \"{webUri}\");";
						Console.WriteLine($"JS:{js}");
						var result = WebAssemblyRuntime.InvokeJS(js);
					});
	}
#endif
}
