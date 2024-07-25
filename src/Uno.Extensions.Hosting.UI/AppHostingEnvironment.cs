#if __WASM__
using Uno.Foundation;
using Windows.UI.Core;

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
	public async Task UpdateAddressBar(Uri applicationUri, bool canGoBack)
	{
		CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
					{
						var state = 1;
						if (PlatformHelper.IsWebAssembly)
						{
							state = canGoBack ? 1 : 0;
						}

						var href = Imports.GetLocation();
						var appUriBuilder = new UriBuilder(applicationUri);
						var url = new UriBuilder(href)
						{
							Query = appUriBuilder.Query,
							Path = appUriBuilder.Path
						};
						var webUri = url.Uri.OriginalString;
						// Use state = 1 or 0 to align with the state managed by the SystemNavigationManager (Uno)
						var result = Imports.ReplaceState(state, "", $"{webUri}");
					});
	}
#endif
}

#if __WASM__
internal static partial class Imports
{
	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.Uno.Extensions.Hosting.getLocation")]
	public static partial string GetLocation();


	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.window.history.replaceState")]
	public static partial string ReplaceState(int state, string title, string url);
}
#endif
