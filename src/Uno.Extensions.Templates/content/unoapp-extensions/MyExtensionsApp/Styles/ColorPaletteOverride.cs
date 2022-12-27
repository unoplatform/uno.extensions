//-:cnd:noEmit
namespace MyExtensionsApp.Styles;

public sealed class ColorPaletteOverride : ResourceDictionary
{
	public ColorPaletteOverride()
	{
		this.Build(r => r
			.Add<Color>(Theme.Colors.Primary.Default.Key, "#5B4CF5", "#2F81D8")
			.Add<Color>(Theme.Colors.OnPrimary.Default.Key, "#FFFFFF", "#FFFFFF")
			.Add<Color>(Theme.Colors.Secondary.Default.Key, "#67E5AD", "#FEB839")
			.Add<Color>(Theme.Colors.OnSecondary.Default.Key, "#000000", "#000000")
			.Add<Color>(Theme.Colors.Background.Default.Key, "#F4F4F4", "#000000")
			.Add<Color>(Theme.Colors.OnBackground.Default.Key, "#000000", "#FFFFFF")
			.Add<Color>(Theme.Colors.Surface.Default.Key, "#FFFFFF", "#0F0F0F")
			.Add<Color>(Theme.Colors.OnSurface.Default.Key, "#000000", "#FFFFFF")
			.Add<Color>(Theme.Colors.Error.Default.Key, "#F85977", "#B2213C")
			.Add<Color>(Theme.Colors.OnError.Default.Key, "#FFFFFF", "#FFFFFF"));
	}
}
