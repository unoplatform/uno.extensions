using System;
using System.Threading.Tasks;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;

public interface INavigationRegionService : INavigationService
{
    INavigationService Parent { get; set; }
}

public interface INavigationService
{
    NavigationResponse NavigateAsync(NavigationRequest request);
}

public interface IRegionServiceContainer
{
    Task NavigateAsync(NavigationContext context);
    Task QueuePendingRequest(NavigationRequest pending);
    Task RunPendingNavigation();
    Task AddRegion(string regionName, IRegionServiceContainer childRegion);

    void RemoveRegion(IRegionServiceContainer childRegion);
}

public interface INavigationRegionContainer
{
    INavigationRegionService Navigation { get; }

    IRegionServiceContainer RegionContainer { get; }
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationRegionContainer(INavigationRegionService Navigation, IRegionServiceContainer RegionContainer) : INavigationRegionContainer { }
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
