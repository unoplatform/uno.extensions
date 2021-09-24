namespace Uno.Extensions.Navigation;

public interface INavigationRegionContainer
{
    INavigationRegionService Navigation { get; }

    IRegionServiceContainer RegionContainer { get; }
}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
