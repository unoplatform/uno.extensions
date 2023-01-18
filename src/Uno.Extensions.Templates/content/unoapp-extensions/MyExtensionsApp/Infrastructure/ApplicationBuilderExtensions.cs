//-:cnd:noEmit
namespace MyExtensionsApp;

internal static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder ConfigureResources(this IApplicationBuilder builder)
	{
		// Load WinUI Resources
		builder.App.Resources(r => r.Merged(
			new XamlControlsResources()));

#if useMaterial
		// Load Material Resources
		builder.App.UseMaterial(
			colorOverride: new Styles.ColorPaletteOverride(),
			fontOverride: new Styles.MaterialFontsOverride());

		// Load Uno.UI.Toolkit Resources
		builder.App.Resources(r => r.Merged(
			new ToolkitResources(),
			new MaterialToolkitResources()));
#else
		// Load Uno.UI.Toolkit Resources
		builder.App.Resources(r => r.Merged(
			new ToolkitResources()));
#endif
		return builder;
	}
}
