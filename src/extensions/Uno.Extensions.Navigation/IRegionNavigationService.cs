using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public interface IRegionNavigationService
{
    INavigationService Navigation { get; }

    Task AddRegion(string regionName, IRegionNavigationService childRegion);

    void RemoveRegion(IRegionNavigationService childRegion);
}
