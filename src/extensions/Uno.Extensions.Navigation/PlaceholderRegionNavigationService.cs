using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public class PlaceholderRegionNavigationService : IRegionNavigationService
{
    private IRegionNavigationService _navigationService;

    public IRegionNavigationService NavigationService
    {
        get => _navigationService;
        set
        {
            _navigationService = value;
            foreach (var child in Children)
            {
                _navigationService.Attach(child.Item2, child.Item1);
            }
        }
    }

    private IList<(string, IRegionNavigationService)> Children { get; } = new List<(string, IRegionNavigationService)>();

    public void Attach(IRegionNavigationService childRegion, string regionName)
    {
        var childService = childRegion;
        Children.Add((regionName + string.Empty, childService));
        NavigationService?.Attach(childRegion, regionName);
    }

    public void Detach(IRegionNavigationService childRegion)
    {
        Children.Remove(kvp => kvp.Item2 == childRegion);
        NavigationService?.Detach(childRegion);
    }

    public Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        return NavigationService?.NavigateAsync(request);
    }
}
