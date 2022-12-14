//-:cnd:noEmit
namespace MyExtensionsApp.Styles;

public sealed class ColorPaletteOverride : ResourceDictionary
{
	public ColorPaletteOverride()
	{
		this.Build(r => r
			.Add<Color>("PrimaryColor", "#5B4CF5", "#2F81D8")
			.Add<Color>("OnPrimaryColor", "#FFFFFF", "#FFFFFF")
			.Add<Color>("SecondaryColor", "#67E5AD", "#FEB839")
			.Add<Color>("OnSecondaryColor", "#000000", "#000000")
			.Add<Color>("BackgroundColor", "#F4F4F4", "#000000")
			.Add<Color>("OnBackgroundColor", "#000000", "#FFFFFF")
			.Add<Color>("SurfaceColor", "#FFFFFF", "#0F0F0F")
			.Add<Color>("OnSurfaceColor", "#000000", "#FFFFFF")
			.Add<Color>("ErrorColor", "#F85977", "#B2213C")
			.Add<Color>("OnErrorColor", "#FFFFFF", "#FFFFFF"));
	}
}
