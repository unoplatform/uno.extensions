namespace Uno.Extensions.Navigation;
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationRegionContainer(INavigationRegionService Navigation, IRegionServiceContainer RegionContainer) : INavigationRegionContainer { }
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
