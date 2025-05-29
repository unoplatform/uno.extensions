namespace TestHarness.Ext.Navigation.AddressBar;

#if __WASM__
internal static partial class AddressBarJSImports
{
	[System.Runtime.InteropServices.JavaScript.JSImport("globalThis.Uno.Extensions.Hosting.getLocation")]
	public static partial string GetUrl();
}
#endif
