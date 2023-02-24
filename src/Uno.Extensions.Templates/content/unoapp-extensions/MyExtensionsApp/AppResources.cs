namespace MyExtensionsApp;

public sealed class AppResources : ResourceDictionary
{
	public AppResources()
	{
		// Load WinUI Resources
		this.Build(r => r.Merged(
			new XamlControlsResources()));
#if useToolkit
#if useMaterial

		// Load Uno.UI.Toolkit and Material Resources
		this.Build(r => r.Merged(
			new  MaterialToolkitTheme(
					new Styles.ColorPaletteOverride(), 
					new Styles.MaterialFontsOverride())));
#else

		// Load Uno.UI.Toolkit Resources
		this.Build(r => r.Merged(
			new ToolkitResources()));
#endif
#elif useMaterial

		// Load Material Resources
		this.Build(r => r.Merged(
			new  MaterialTheme(
					new Styles.ColorPaletteOverride(), 
					new Styles.MaterialFontsOverride())));
#endif
	}
}
