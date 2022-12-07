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
				.Build(mc => mc.Merged(new Styles.ColorPaletteOverride())),
			new MaterialFonts()
				.Build(mf => mf.Merged(new Styles.MaterialFontsOverride())),
			new MaterialResources(),

			// Load Uno.UI.Toolkit resources
			new ToolkitResources(),
			new MaterialToolkitResources()
		));
		return builder;
	}
}
