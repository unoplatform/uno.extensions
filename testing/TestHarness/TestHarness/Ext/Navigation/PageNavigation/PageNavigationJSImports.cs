namespace TestHarness.Ext.Navigation.PageNavigation;

#if __WASM__
internal static partial class PageNavigationJSImports
{
	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.Uno.Extensions.Hosting.getLocation")]
	public static partial string GetLocation();
}
#endif
