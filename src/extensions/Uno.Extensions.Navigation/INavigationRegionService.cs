namespace Uno.Extensions.Navigation;

public interface INavigationRegionService : INavigationService
{
    INavigationService Parent { get; set; }
}
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
