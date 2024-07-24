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

	private List<string> history = new List<string>();

	public Assembly? HostAssembly { get; init; }

#if __WASM__
	public async Task UpdateAddressBar(Uri applicationUri)
	{
		CoreApplication.MainView?.DispatcherQueue.TryEnqueue(() =>
					{
						var state = 1;
						if (PlatformHelper.IsWebAssembly)
						{
							var currentView = SystemNavigationManager.GetForCurrentView();
							state = currentView?.AppViewBackButtonVisibility == AppViewBackButtonVisibility.Visible ? 1 : 0;

							Imports.DisplayMessage($"UpdateAddressBar - State: {state}");
						}

						var href = Imports.GetLocation();

						Imports.DisplayMessage($"UpdateAddressBar - Href: {href}");

						var appUriBuilder = new UriBuilder(applicationUri);
						var url = new UriBuilder(href)
						{
							Query = appUriBuilder.Query,
							Path = appUriBuilder.Path
						};
						var webUri = url.Uri.OriginalString;

						Imports.DisplayMessage($"UpdateAddressBar - WebUri: {webUri}");

						string path = url.Uri.AbsolutePath;
						string routeName = path.TrimStart('/');

						Imports.DisplayMessage($"UpdateAddressBar - routeName: {routeName}");

						// Use state = 1 or 0 to align with the state managed by the SystemNavigationManager (Uno)

						if(history.Count == 0)
						{
							Imports.ReplaceState(routeName, "", $"{webUri}");
							history.Add(routeName);
							Imports.DisplayMessage($"UpdateAddressBar - Start: history.Add: {routeName}");
						}
						else if (history.Contains(routeName))
						{
							Imports.ReplaceState(routeName, "", $"{webUri}");
							history.Remove(routeName);
							Imports.DisplayMessage($"UpdateAddressBar - history.Remove: {routeName}");
						}
						else
						{
							var result = Imports.PushState(routeName, "", $"{webUri}");
							history.Add(routeName);
							Imports.DisplayMessage($"UpdateAddressBar - history.Add: {routeName}");
						}
					});
	}
#endif
}

#if __WASM__
internal static partial class Imports
{
	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.Uno.Extensions.Hosting.getLocation")]
	public static partial string GetLocation();

	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.window.history.pushState")]
	public static partial string PushState(string state, string title, string url);

	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.window.history.replaceState")]
	public static partial string ReplaceState(string state, string title, string url);

	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.Uno.Extensions.Hosting.displayMessage")]
	public static partial string DisplayMessage(string message);
}
#endif
