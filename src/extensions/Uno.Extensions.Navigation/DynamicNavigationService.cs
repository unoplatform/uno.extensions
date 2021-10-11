using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Navigation.Regions;

namespace Uno.Extensions.Navigation;
public class DynamicNavigationService : CompositeNavigationService
{
    public IRegion Region { get; set; }

    public DynamicNavigationService(ILogger<RegionNavigationService> logger) : base(logger)
    {
    }

    public override Task<NavigationResponse> NavigateAsync(NavigationRequest request)
    {
        return Region.NavigateAsync(request);
    }
}
