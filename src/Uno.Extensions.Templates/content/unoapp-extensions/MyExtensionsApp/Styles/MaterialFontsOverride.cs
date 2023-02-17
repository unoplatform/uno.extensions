//-:cnd:noEmit
namespace MyExtensionsApp.Styles;

public sealed class MaterialFontsOverride : ResourceDictionary
{
	public MaterialFontsOverride()
	{
		this.Build(r => r
			.Add<FontFamily>("MaterialLightFontFamily", "ms-appx:///Uno.Fonts.Roboto/Fonts/Material/Roboto-Light.ttf#Roboto")
			.Add<FontFamily>("MaterialMediumFontFamily", "ms-appx:///Uno.Fonts.Roboto/Fonts/Material/Roboto-Medium.ttf#Roboto")
			.Add<FontFamily>("MaterialRegularFontFamily", "ms-appx:///Uno.Fonts.Roboto/Fonts/Material/Roboto-Regular.ttf#Roboto"));
	}
}