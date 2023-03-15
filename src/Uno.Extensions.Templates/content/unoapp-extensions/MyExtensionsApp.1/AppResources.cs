namespace MyExtensionsApp._1;

public sealed class AppResources : ResourceDictionary
{
	public AppResources()
	{
		// Load WinUI Resources
		this.Build(r => r.Merged(
			new XamlControlsResources()));
#if useMaterial

		// Load Uno.UI.Toolkit and Material Resources
		this.Build(r => r.Merged(
			new  MaterialToolkitTheme(
					new Styles.ColorPaletteOverride(),
					new Styles.MaterialFontsOverride())));
#elif (useRecommendedAppTemplate)

		// Load Uno.UI.Toolkit Resources
		this.Build(r => r.Merged(
			new ToolkitResources()));
#endif
	}
}
