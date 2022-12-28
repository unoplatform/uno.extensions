//-:cnd:noEmit
namespace MyExtensionsApp;

public static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder ConfigureResources(this IApplicationBuilder builder)
	{
		builder.App.Resources(r => r.Merged(
			// Load WinUI Resources
			new XamlControlsResources(),

			// Load Material Resources
			new MaterialColors()
			{
				OverrideDictionary = new Styles.ColorPaletteOverride()
			},
			new MaterialFonts()
			{
				OverrideDictionary = new Styles.MaterialFontsOverride()
			},
			new MaterialResources(),

			// Load Uno.UI.Toolkit resources
			new ToolkitResources(),
			new MaterialToolkitResources()
		));
		return builder;
	}
}
