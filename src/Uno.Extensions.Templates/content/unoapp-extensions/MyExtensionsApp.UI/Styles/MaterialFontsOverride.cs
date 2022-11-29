//-:cnd:noEmit
namespace MyExtensionsApp.Styles;

public sealed class MaterialFontsOverride : ResourceDictionary
{
	public MaterialFontsOverride()
	{
		this.Build(r => r
			.Add<FontFamily>("MaterialLightFontFamily", "ms-appx:///Assets/Fonts/Material/Roboto-Light.ttf#Roboto")
			.Add<FontFamily>("MaterialMediumFontFamily", "ms-appx:///Assets/Fonts/Material/Roboto-Medium.ttf#Roboto")
			.Add<FontFamily>("MaterialRegularFontFamily", "ms-appx:///Assets/Fonts/Material/Roboto-Regular.ttf#Roboto"));
	}
}
